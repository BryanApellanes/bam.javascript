using Bam.Javascript;
using Bam.Net;
using Bam.Net.Data;
using Bam.Net.Data.Schema;
using Bam.Net.Javascript;
//using Bam.Net.Javascript;
using Newtonsoft.Json;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Data.Schema
{
    // TODO: Determine if this class is worth keeping;  I don't think it is
    // if so convert it to use RoslynCompiler instead of AdHocCsharpCompiler see TypeToDaoGenerator.Compile

    public class DaoSchema
    {
        public static IDaoCodeWriter DaoCodeWriter { get; set; }

        static object _schemaManagerLock = new object();
        static JsLiteralSchemaManager _schemaManager;
        public static JsLiteralSchemaManager GetSchemaManager()
        {
            return _schemaManagerLock.DoubleCheckLock(ref _schemaManager, () => new JsLiteralSchemaManager());
        }

        public static Func<IDaoSchemaDefinition, string> SchemaTempPathProvider { get; set; }

        static string _rootDir;
        public static string RootDir
        {
            get
            {
                return _rootDir ?? SchemaTempPathProvider(GetSchemaManager().CurrentSchema);
            }

            set
            {
                _rootDir = value;
            }
        }

        public static string BinDir => Path.Combine(RootDir, "bin");
        /// <summary>
        /// Gets the most recent set of exceptions that occurred during an attempted
        /// Generate -> Compile
        /// </summary>
        public static CompilerErrorCollection CompilerErrors
        {
            get;
            private set;
        }

        public static CompilerError[] GetErrors()
        {
            if (CompilerErrors == null)
            {
                return new CompilerError[] { };
            }

            CompilerError[] results = new CompilerError[CompilerErrors.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = CompilerErrors[i];
            }

            return results;
        }

        public static SchemaManagerResult GenerateAssembly(FileInfo dbJs, DirectoryInfo compileTo, DirectoryInfo tempSourceDir)
        {
            JsLiteralSchemaManager schemaManager = GetSchemaManager();

            DirectoryInfo partialsDir = new DirectoryInfo(Path.Combine(dbJs.Directory.FullName, "DaoPartials"));
            SchemaManagerResult schemaManagerResult = new SchemaManagerResult("Generator Not Run, invalid file extension", false);
            if (dbJs.Extension.ToLowerInvariant().Equals(".js"))
            {
                schemaManagerResult = GenerateDaoAssembly(schemaManager, dbJs, compileTo, tempSourceDir, partialsDir);
            }
            else if (dbJs.Extension.ToLowerInvariant().Equals(".json"))
            {
                string json = File.ReadAllText(dbJs.FullName);
                schemaManagerResult = GenerateDaoAssembly(schemaManager, json, compileTo, tempSourceDir);
            }

            return schemaManagerResult;
        }

        // TOOD: extract the generator functionality and move to DaoSchema

        public static SchemaManagerResult GenerateDaoAssembly(JsLiteralSchemaManager schemaManager, FileInfo databaseDotJs, DirectoryInfo compileTo, DirectoryInfo temp, DirectoryInfo partialsDir)
        {
            string databaseSchemaJson = databaseDotJs.JsonFromJsLiteralFile("database");
            return GenerateDaoAssembly(schemaManager, databaseSchemaJson, compileTo, false, temp.FullName, partialsDir.FullName);
        }

        public static SchemaManagerResult GenerateDaoAssembly(JsLiteralSchemaManager schemaManager, string simpleSchemaJson, DirectoryInfo compileTo, DirectoryInfo temp)
        {
            return GenerateDaoAssembly(schemaManager, simpleSchemaJson, compileTo, false, temp.FullName);
        }

        /// <summary>
        /// Generate 
        /// </summary>
        /// <param name="simpleSchemaJson"></param>
        /// <returns></returns>
        public static SchemaManagerResult GenerateDaoAssembly(JsLiteralSchemaManager schemaManager, string simpleSchemaJson, DirectoryInfo compileTo = null, bool keepSource = false, string tempDir = "./tmp", string partialsDir = null)
        {
            try
            {
                bool compile = compileTo != null;
                SchemaManagerResult managerResult = new SchemaManagerResult("Generation completed");
                dynamic rehydrated = JsonConvert.DeserializeObject<dynamic>(simpleSchemaJson);
                if (rehydrated["nameSpace"] == null)// || rehydrated["schemaName"] == null)
                {
                    managerResult.ExceptionMessage = "Please specify nameSpace";
                    managerResult.Message = string.Empty;
                    managerResult.Success = false;
                }
                else if (rehydrated["schemaName"] == null)
                {
                    managerResult.ExceptionMessage = "Please specify schemaName";
                    managerResult.Message = string.Empty;
                    managerResult.Success = false;
                }
                else
                {
                    string nameSpace = (string)rehydrated["nameSpace"];
                    string schemaName = (string)rehydrated["schemaName"];
                    managerResult.Namespace = nameSpace;
                    managerResult.SchemaName = schemaName;
                    List<dynamic> foreignKeys = new List<dynamic>();

                    schemaManager.SetSchema(schemaName, false);

                    schemaManager.ProcessTables(rehydrated, foreignKeys);
                    schemaManager.ProcessXrefs(rehydrated, foreignKeys);

                    foreach (dynamic fk in foreignKeys)
                    {
                        schemaManager.AddColumn(fk.ForeignKeyTable, new Column(fk.ReferencingColumn, DataTypes.ULong));
                        schemaManager.SetForeignKey(fk.PrimaryTable, fk.ForeignKeyTable, fk.ReferencingColumn);
                    }

                    DirectoryInfo daoDir = new DirectoryInfo(tempDir);
                    if (!daoDir.Exists)
                    {
                        daoDir.Create();
                    }

                    DaoGenerator generator = GetDaoGenerator(compileTo, keepSource, partialsDir, compile, managerResult, nameSpace, daoDir);
                    generator.Generate(schemaManager.CurrentSchema, daoDir.FullName, partialsDir);
                    managerResult.DaoAssembly = generator.DaoAssemblyFile;
                }
                return managerResult;
            }
            catch (Exception ex)
            {
                SchemaManagerResult r = new SchemaManagerResult(ex.Message)
                {
                    StackTrace = ex.StackTrace ?? "",
                    Success = false
                };
                return r;
            }
        }

        private static DaoGenerator GetDaoGenerator(DirectoryInfo compileTo, bool keepSource, string partialsDir, bool compile, SchemaManagerResult managerResult, string nameSpace, DirectoryInfo daoDir)
        {
            Args.ThrowIfNull(DaoCodeWriter, "DaoCodeWriter");

            DaoGenerator generator = new DaoGenerator(DaoCodeWriter, nameSpace);
            if (compile)
            {
                if (!compileTo.Exists)
                {
                    compileTo.Create();
                }

                generator.GenerateComplete += (gen, s) =>
                {
                    List<DirectoryInfo> daoDirs = new List<DirectoryInfo> { daoDir };
                    if (!string.IsNullOrEmpty(partialsDir))
                    {
                        daoDirs.Add(new DirectoryInfo(partialsDir));
                    }

                    gen.DaoAssemblyFile = Compile(daoDirs.ToArray(), gen, nameSpace, compileTo);

                    if (CompilerErrors != null)
                    {
                        managerResult.Success = false;
                        managerResult.Message = string.Empty;
                        foreach (CompilerError err in GetErrors())
                        {
                            managerResult.Message = $"{managerResult.Message}\r\nFile=>{err.FileName}\r\n{err.ErrorNumber}:::Line {err.Line}, Column {err.Column}::{err.ErrorText}";
                        }
                    }
                    else
                    {
                        managerResult.Message = $"{managerResult.Message}\r\nDao Compiled";
                        managerResult.Success = true;
                    }

                    if (!keepSource)
                    {
                        daoDir.Delete(true);
                        daoDir.Refresh();
                        if (daoDir.Exists)
                        {
                            daoDir.Delete();
                        }
                    }
                };
            }
            return generator;
        }

        private static FileInfo Compile(DirectoryInfo[] dirs, DaoGenerator generator, string nameSpace, DirectoryInfo copyTo)
        {
            string[] referenceAssemblies = DaoGenerator.DefaultReferenceAssemblies.ToArray();
            for (int i = 0; i < referenceAssemblies.Length; i++)
            {
                string assembly = referenceAssemblies[i];
                string binPath = Path.Combine(BinDir, assembly);

                referenceAssemblies[i] = File.Exists(binPath) ? binPath : assembly;
            }

            CompilerResults results = AdHocCSharpCompiler.CompileDirectories(dirs, $"{nameSpace}.dll", referenceAssemblies, false);
            if (results.Errors.Count > 0)
            {
                CompilerErrors = results.Errors;
                return null;
            }
            else
            {
                CompilerErrors = null;
                DirectoryInfo bin = new DirectoryInfo(BinDir);
                if (!bin.Exists)
                {
                    bin.Create();
                }

                FileInfo dll = new FileInfo(results.CompiledAssembly.CodeBase.Replace("file:///", ""));

                string binFile = Path.Combine(bin.FullName, dll.Name);
                string copy = Path.Combine(copyTo.FullName, dll.Name);
                if (File.Exists(binFile))
                {
                    BackupFile(binFile);
                }
                dll.CopyTo(binFile, true);
                if (!binFile.ToLowerInvariant().Equals(copy.ToLowerInvariant()))
                {
                    if (File.Exists(copy))
                    {
                        BackupFile(copy);
                    }

                    dll.CopyTo(copy);
                }

                return new FileInfo(copy);
            }
        }
        private static void BackupFile(string fileName)
        {
            FileInfo binFileInfo = new FileInfo(fileName);
            FileInfo backupFile = new FileInfo(Path.Combine(
                        binFileInfo.Directory.FullName,
                        "backup",
                        $"{Path.GetFileNameWithoutExtension(fileName)}_{"".RandomLetters(4)}_{DateTime.Now.ToJulianDate().ToString()}.dll"));

            if (!backupFile.Directory.Exists)
            {
                backupFile.Directory.Create();
            }
            binFileInfo.MoveTo(backupFile.FullName);
        }
    }
}
