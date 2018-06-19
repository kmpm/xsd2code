using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xsd2Code.Library;
using Xsd2Code.Library.Helpers;
using Xsd2Code.TestUnit.Properties;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;

namespace Xsd2Code.TestUnit
{
    /// <summary>
    /// Xsd2Code unit tests
    /// </summary>
    /// <remarks>
    /// Revision history:
    /// 
    ///     Modified 2009-02-25 by Ruslan Urban 
    ///     Performed code review
    ///     Changed output folder to the TestResults folder to preserve files in the testing history
    ///     TODO: Add tests that compile generated code
    /// 
    /// </remarks>
    [TestClass]
    public class UnitTest
    {
        private readonly object testLock = new object();
        static readonly object fileLock = new object();

        /// <summary>
        /// Output folder: TestResults folder relative to the solution root folder
        /// </summary>
        private static string OutputFolder
        {
            get { return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"testtemp"); }
        }

        /// <summary>
        /// Code generation namespace  
        /// </summary>
        private const string CodeGenerationNamespace = "Xsd2Code.TestUnit";

        #region Additional test attributes

        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //

        #endregion

        /// <summary>
        /// Circulars this instance.
        /// </summary>
        [TestMethod]
        public void Circular()
        {
            lock (testLock)
            {
                // Copy resource file to the run-time directory
                string inputFilePath = GetInputFilePath("Circular.xsd", Resources.Circular);

                var xsdGen = new GeneratorFacade(GetGeneratorParams(inputFilePath));
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());
            }
        }

        /// <summary>
        /// Circulars this instance.
        /// </summary>
        [TestMethod]
        public void CircularClassReference()
        {
            lock (testLock)
            {

                // Copy resource file to the run-time directory
                string inputFilePath = GetInputFilePath("CircularClassReference.xsd", Resources.CircularClassReference);
                var generatorParams = new GeneratorParams
                                          {
                                              InputFilePath = inputFilePath,
                                              TargetFramework = TargetFramework.Net20,
                                              OutputFilePath = GetOutputFilePath(inputFilePath)

                                          };
                generatorParams.PropertyParams.AutomaticProperties = true;
                generatorParams.Serialization.Enabled = false;
                generatorParams.GenericBaseClass.Enabled = false;

                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                try
                {
                    var cs = new Circular();

#pragma warning disable 168
                    int count = cs.circular.count;
#pragma warning restore 168

                    var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                    Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
                }
                catch (Exception e)
                {
                    Assert.Fail(e.Message);
                }
            }
        }

        /// <summary>
        /// Arrays the of array.
        /// </summary>
        [TestMethod]
        public void ArrayOfArray()
        {
            lock (testLock)
            {

                // Copy resource file to the run-time directory
                var inputFilePath = GetInputFilePath("ArrayOfArray.xsd", Resources.ArrayOfArray);

                var generatorParams = new GeneratorParams
                                          {
                                              GenerateCloneMethod = true,
                                              InputFilePath = inputFilePath,
                                              NameSpace = "MyNameSpace",
                                              CollectionObjectType = CollectionType.Array,
                                              EnableDataBinding = true,
                                              Language = GenerationLanguage.CSharp,
                                              OutputFilePath = Path.ChangeExtension(inputFilePath, ".TestGenerated.cs")
                                          };
                generatorParams.PropertyParams.AutomaticProperties = true;
                generatorParams.Serialization.Enabled = true;
                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
            }
        }

        /// <summary>
        /// Stacks the over flow.
        /// </summary>
        [TestMethod]
        public void StackOverFlow()
        {
            lock (testLock)
            {

                // Copy resource file to the run-time directory
                string inputFilePath = GetInputFilePath("StackOverFlow.xsd", Resources.StackOverFlow);

                var generatorParams = GetGeneratorParams(inputFilePath);
                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
            }
        }

        [TestMethod]
        public void Deserialize_ArrayOfMyElement()
        {
            lock (testLock)
            {

                var e = new ArrayOfMyElement();
                var myE = new MyElement { Name = "Name" };
                myE.AttributeLists.Add(new NameValuePair { Name = "Name", Value = "Value" });
                e.MyElement.Add(myE);
                Exception ex;

                var serialized = e.Serialize();
                e.SaveToFile(Path.Combine(OutputFolder, "ReproSampleFile.xml"), out ex);
                if (ex != null) throw ex;

                //try to deserialize

                //generate doc conformant to schema

                ArrayOfMyElement toDeserialize;
                if (!ArrayOfMyElement.LoadFromFile("ReproSampleFile.xml", out toDeserialize, out ex))
                {
                    Console.WriteLine("Unable to deserialize, will exit");
                    return;
                }

                var serialized2 = toDeserialize.Serialize();

                Console.WriteLine("Still missing the <NameValuePairElement>");
                Console.WriteLine(serialized);

                Console.WriteLine("Name value pairs elements missing");
                Console.WriteLine(serialized2);
            }
        }

        /// <summary>
        /// DVDs this instance.
        /// </summary>
        [TestMethod]
        public void Dvd()
        {
            lock (testLock)
            {

                // Copy resource file to the run-time directory
                GetInputFilePath("Actor.xsd", Resources.Actor);

                // Copy resource file to the run-time directory
                string inputFilePath = GetInputFilePath("Dvd.xsd", Resources.dvd);
                var generatorParams = GetGeneratorParams(inputFilePath);
                generatorParams.CollectionObjectType = CollectionType.List;
                generatorParams.TargetFramework = TargetFramework.Net35;
                generatorParams.EnableDataBinding = true;
                generatorParams.Miscellaneous.EnableSummaryComment = true;
                generatorParams.GenerateDataContracts = false;
                generatorParams.GenericBaseClass.Enabled = false;
                generatorParams.Serialization.GenerateXmlAttributes = true;
                generatorParams.TrackingChanges.Enabled = false;
                generatorParams.TrackingChanges.GenerateTrackingClasses = false;
                generatorParams.Serialization.EnableEncoding = false;
                generatorParams.Serialization.DefaultEncoder = DefaultEncoder.UTF8;

                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                // Create new dvd collection and save it to file
                var dvd = new DvdCollection();
                dvd.Dvds.Add(new dvd { Title = "Matrix È‡?" });
                var newitem = new dvd();
                newitem.Actor.Add(new Actor { firstname = "JamÈs ‡&", nationality = "Us" });
                dvd.Dvds.Add(newitem);
                var originalXml = dvd.Serialize();
                var dvdFile = Path.Combine(OutputFolder, "dvd.xml");
                dvd.SaveToFile(dvdFile);

                // Load data fom file and serialize it again.                                                                                                                                                               

                var loadedDvdCollection = DvdCollection.LoadFromFile(dvdFile);
                var finalXml = loadedDvdCollection.Serialize();

                // Then comprate two xml string
                if (!originalXml.Equals(finalXml))
                {
                    Assert.Fail("Xml value are not equals");
                }
                Exception exp;
                DvdCollection deserialiseDvd;

                var testEncodings = new [] {
                  Encoding.ASCII, Encoding.UTF8, Encoding.Unicode, Encoding.UTF32
                };
                foreach (var encoding in testEncodings)
                {
                  var encodedFile = Path.Combine(OutputFolder, "dvd" + encoding.EncodingName + ".xml");
                  dvd.SaveToFile(encodedFile, encoding);
                  if (!DvdCollection.LoadFromFile(encodedFile, encoding, out deserialiseDvd, out exp))
                  {
                    Assert.Fail("LoadFromFile failed on {0} encoding", encoding.EncodingName);
                  }
                  else if (!deserialiseDvd.Dvds[0].Title.Equals("Matrix È‡?"))
                  {
                   Assert.Fail("LoadFromFile failed on {0} encoding", encoding.EncodingName);
                  }                
                }
                
                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());

            }
        }


        /// <summary>
        /// Test LazyLoadind pattern
        /// </summary>
        [TestMethod]
        public void LazyLoading()
        {
            lock (testLock)
            {
                // Copy resource file to the run-time directory
                string inputFilePath = GetInputFilePath("LazyLoading.xsd", Resources.LazyLoading);
                var generatorParams = GetGeneratorParams(inputFilePath);
                generatorParams.CollectionObjectType = CollectionType.List;
                generatorParams.TargetFramework = TargetFramework.Net20;
                generatorParams.PropertyParams.EnableLazyLoading = true;
                generatorParams.EnableInitializeFields = true;

                var xsdGen = new GeneratorFacade(generatorParams);
                xsdGen.Generate();

                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
            }
        }

        /// <summary>
        /// Test PropertyNameSpecified
        /// </summary>
        [TestMethod]
        public void PropertyNameSpecified()
        {
            lock (testLock)
            {
                // Copy resource file to the run-time directory
                string inputFilePath = GetInputFilePath("PropertyNameSpecified.xsd", Resources.PropertyNameSpecified);
                var generatorParams = GetGeneratorParams(inputFilePath);
                generatorParams.TargetFramework = TargetFramework.Net20;
                generatorParams.Serialization.Enabled = false;
                generatorParams.Miscellaneous.HidePrivateFieldInIde = false;

                // All
                generatorParams.PropertyParams.GeneratePropertyNameSpecified = PropertyNameSpecifiedType.All;
                generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".all.cs");
                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());

                // none
                generatorParams.PropertyParams.GeneratePropertyNameSpecified = PropertyNameSpecifiedType.None;
                generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".none.cs");
                xsdGen = new GeneratorFacade(generatorParams);
                result = xsdGen.Generate();

                compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());

                // Default
                generatorParams.PropertyParams.GeneratePropertyNameSpecified = PropertyNameSpecifiedType.Default;
                generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".default.cs");
                xsdGen = new GeneratorFacade(generatorParams);
                result = xsdGen.Generate();

                compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
            }
        }

        /// <summary>
        /// Genders this instance.
        /// </summary>
        [TestMethod]
        public void Gender()
        {
            lock (testLock)
            {

                // Get the code namespace for the schema.
                string inputFilePath = GetInputFilePath("Gender.xsd", Resources.Gender);

                var generatorParams = GetGeneratorParams(inputFilePath);
                generatorParams.TargetFramework = TargetFramework.Net30;
                generatorParams.PropertyParams.AutomaticProperties = true;
                generatorParams.GenerateDataContracts = true;
                generatorParams.Serialization.GenerateXmlAttributes = true;
                generatorParams.OutputFilePath = GetOutputFilePath(inputFilePath);

                var xsdGen = new GeneratorFacade(generatorParams);

                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                var genderRoot = new Root
                                     {
                                         GenderAttribute = ksgender.female,
                                         GenderAttributeSpecified = true,
                                         GenderElement = ksgender.female,
                                         GenderIntAttribute = "toto"
                                     };
                Exception ex;
                genderRoot.SaveToFile(Path.Combine(OutputFolder, "gender.xml"), out ex);
                if (ex != null) throw ex;

                var canCompile = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(canCompile.Success, canCompile.Messages.ToString());
            }
        }

        [TestMethod]
        public void GenarateVBCS()
        {
            lock (testLock)
            {
                // Get the code namespace for the schema.
                string inputFilePath = GetInputFilePath("Actor.xsd", Resources.Actor);

                var generatorParams = GetGeneratorParams(inputFilePath);
                generatorParams.TargetFramework = TargetFramework.Net30;
                generatorParams.PropertyParams.AutomaticProperties = true;
                generatorParams.GenerateDataContracts = true;
                generatorParams.Serialization.GenerateXmlAttributes = true;
                generatorParams.OutputFilePath = GetOutputFilePath(inputFilePath);
                generatorParams.EnableDataBinding = true;
                generatorParams.Miscellaneous.EnableSummaryComment = true;
                generatorParams.Language = GenerationLanguage.VisualBasic;
                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();
                Assert.IsTrue(result.Success, result.Messages.ToString());

                generatorParams.Language = GenerationLanguage.CSharp;
                xsdGen = new GeneratorFacade(generatorParams);
                result = xsdGen.Generate();

                var canCompile = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(canCompile.Success, canCompile.Messages.ToString());
            }
        }
        /// <summary>
        /// Alows the debug.
        /// </summary>
        [TestMethod]
        public void AlowDebug()
        {
            lock (testLock)
            {

                // Copy resource file to the run-time directory
                GetInputFilePath("Actor.xsd", Resources.Actor);
                string inputFilePath = GetInputFilePath("Dvd.xsd", Resources.dvd);

                var generatorParams = GetGeneratorParams(inputFilePath);
                generatorParams.Miscellaneous.DisableDebug = false;
                generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".DebugEnabled.cs");

                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
            }
        }

        [TestMethod]
        public void Hierarchical()
        {
            lock (testLock)
            {

                // Copy resource file to the run-time directory
                string inputFilePath = GetInputFilePath("Hierarchical.xsd", Resources.Hierarchical);

                var generatorParams = GetGeneratorParams(inputFilePath);
                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
            }
        }

        [TestMethod]
        public void HexBinary()
        {
            lock (testLock)
            {

                // Copy resource file to the run-time directory
                string inputFilePath = GetInputFilePath("hexBinary.xsd", Resources.hexBinary);

                var generatorParams = GetGeneratorParams(inputFilePath);
                generatorParams.EnableInitializeFields = true;
                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
            }
        }

        //[TestMethod]
        //public void Serialize()
        //{
        //    DvdCollection dvdCol = GetDvd();
        //    string dvdColStr1 = dvdCol.Serialize();

        //    DvdCollection dvdColFromXml;
        //    Exception exception;
        //    bool sucess = DvdCollection.Deserialize(dvdColStr1, out dvdColFromXml, out exception);
        //    if (sucess)
        //    {
        //        string dvdColStr2 = dvdColFromXml.Serialize();
        //        if (!dvdColStr1.Equals(dvdColStr2))
        //            Assert.Fail("dvdColFromXml is not equal after Deserialize");
        //    }
        //    else
        //        Assert.Fail(exception.Message);
        //}

        [TestMethod]
        public void Silverlight()
        {
            lock (testLock)
            {

                // Get the code namespace for the schema.
                GetInputFilePath("Actor.xsd", Resources.Actor);
                string inputFilePath = GetInputFilePath("dvd.xsd", Resources.dvd);

                var generatorParams = GetGeneratorParams(inputFilePath);
                generatorParams.TargetFramework = TargetFramework.Silverlight;
                generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath,
                                                                      ".Silverlight20_01.cs");

                generatorParams.Serialization.Enabled = true;
                generatorParams.Serialization.EnableEncoding = true;
                var xsdGen = new GeneratorFacade(generatorParams);

                var result = xsdGen.Generate();
                Assert.IsTrue(result.Success, result.Messages.ToString());

            }
        }


        [TestMethod]
        public void XMLAttributes()
        {
            lock (testLock)
            {

                // Get the code namespace for the schema.
                GetInputFilePath("Actor.xsd", Resources.Actor);
                string inputFilePath = GetInputFilePath("mailxml_base.xsd", Resources.mailxml_base);
                GetInputFilePath("mailxml_base_120108.xsd", Resources.mailxml_base_120108);

                var generatorParams = GetGeneratorParams(inputFilePath);
                generatorParams.TargetFramework = TargetFramework.Net30;
                generatorParams.PropertyParams.AutomaticProperties = true;
                generatorParams.Serialization.GenerateXmlAttributes = true;
                generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".xml.cs");

                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                //var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                //Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
            }
        }

        [TestMethod]
        public void AutomaticProperties()
        {
            lock (testLock)
            {

                // Get the code namespace for the schema.
                GetInputFilePath("Actor.xsd", Resources.Actor);
                string inputFilePath = GetInputFilePath("dvd.xsd", Resources.dvd);

                var generatorParams = new GeneratorParams { InputFilePath = inputFilePath };
                GetGeneratorParams(inputFilePath);
                generatorParams.TargetFramework = TargetFramework.Net30;
                generatorParams.Miscellaneous.EnableSummaryComment = false;
                generatorParams.GenerateDataContracts = false;
                generatorParams.PropertyParams.AutomaticProperties = true;
                generatorParams.Serialization.GenerateXmlAttributes = true;
                generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".autoProp.cs");

                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
            }
        }

        [TestMethod]
        public void UseBaseClass()
        {
            lock (testLock)
            {

                // Get the code namespace for the schema.
                GetInputFilePath("Actor.xsd", Resources.Actor);
                string inputFilePath = GetInputFilePath("dvd.xsd", Resources.dvd);

                string outputFilePath = Path.ChangeExtension(inputFilePath, ".baseClass.cs");
                var generatorParams = new GeneratorParams
                                          {
                                              InputFilePath = inputFilePath,
                                              TargetFramework = TargetFramework.Net30,
                                              GenerateDataContracts = true,
                                              EnableDataBinding = true,
                                              OutputFilePath = outputFilePath
                                          };

                generatorParams.PropertyParams.AutomaticProperties = false;
                generatorParams.Miscellaneous.EnableSummaryComment = true;
                generatorParams.GenericBaseClass.Enabled = true;
                generatorParams.GenericBaseClass.GenerateBaseClass = true;
                generatorParams.GenericBaseClass.BaseClassName = "EntityObject";

                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
                
                // check if autogeneration-parameters are written to file
                var lastGenerationParamsFile = Path.ChangeExtension(inputFilePath, "xsd.xsd2code");
                if (File.Exists(lastGenerationParamsFile)) {
                    if (File.GetLastWriteTime(lastGenerationParamsFile) > File.GetLastWriteTime(generatorParams.OutputFilePath))
                    {
                        File.Delete(lastGenerationParamsFile);
                        File.Copy(generatorParams.OutputFilePath, lastGenerationParamsFile);
                    }
                } else {
                    File.Copy(generatorParams.OutputFilePath, lastGenerationParamsFile);
                }

                var autogenParams = GeneratorParams.LoadFromFile(inputFilePath);
                Assert.AreEqual(autogenParams.TargetFramework, generatorParams.TargetFramework);
                Assert.AreEqual(autogenParams.GenerateDataContracts, generatorParams.GenerateDataContracts);
                Assert.AreEqual(autogenParams.EnableDataBinding, generatorParams.EnableDataBinding);
                Assert.AreEqual(autogenParams.PropertyParams.AutomaticProperties, generatorParams.PropertyParams.AutomaticProperties);
                Assert.AreEqual(autogenParams.Miscellaneous.EnableSummaryComment, generatorParams.Miscellaneous.EnableSummaryComment);
                Assert.AreEqual(autogenParams.GenericBaseClass.Enabled, generatorParams.GenericBaseClass.Enabled);
                Assert.AreEqual(autogenParams.GenericBaseClass.GenerateBaseClass, generatorParams.GenericBaseClass.GenerateBaseClass);
                Assert.AreEqual(autogenParams.GenericBaseClass.BaseClassName, generatorParams.GenericBaseClass.BaseClassName);
                File.Delete(lastGenerationParamsFile);
            }
        }

        [TestMethod]
        public void TestAnnotations()
        {
            lock (testLock)
            {

                // Get the code namespace for the schema.
                string inputFilePath = GetInputFilePath("TestAnnotations.xsd", Resources.TestAnnotations);

                var generatorParams = new GeneratorParams { InputFilePath = inputFilePath };
                GetGeneratorParams(inputFilePath);

                generatorParams.Miscellaneous.EnableSummaryComment = true;
                generatorParams.TargetFramework = TargetFramework.Net35;
                generatorParams.PropertyParams.AutomaticProperties = true;
                generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath,
                                                                      ".TestAnnotations.cs");

                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
            }
        }

        [TestMethod]
        public void WcfAttributes()
        {
            lock (testLock)
            {

                // Get the code namespace for the schema.
                GetInputFilePath("Actor.xsd", Resources.Actor);
                string inputFilePath = GetInputFilePath("dvd.xsd", Resources.dvd);

                var generatorParams = GetGeneratorParams(inputFilePath);
                generatorParams.GenerateDataContracts = true;
                generatorParams.TargetFramework = TargetFramework.Net30;
                generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".wcf.cs");

                var xsdGen = new GeneratorFacade(generatorParams);
                var result = xsdGen.Generate();

                Assert.IsTrue(result.Success, result.Messages.ToString());

                var compileResult = CompileCSFile(generatorParams.OutputFilePath);
                Assert.IsTrue(compileResult.Success, compileResult.Messages.ToString());
            }
        }

        //[TestMethod]
        //public void Persistent()
        //{
        //    DvdCollection dvdCol = GetDvd();
        //    Exception exception;
        //    if (!dvdCol.SaveToFile(OutputFolder + @"savedvd.xml", out exception))
        //        Assert.Fail(string.Format("Failed to save file. {0}", exception.Message));

        //    DvdCollection loadedDvdCollection;
        //    Exception e;
        //    if (!DvdCollection.LoadFromFile(OutputFolder + @"savedvd.xml", out loadedDvdCollection, out e))
        //        Assert.Fail(string.Format("Failed to load file. {0}", e.Message));

        //    string xmlBegin = dvdCol.Serialize();
        //    string xmlEnd = loadedDvdCollection.Serialize();

        //    if (!xmlBegin.Equals(xmlEnd))
        //        Assert.Fail(string.Format("xmlBegin and xmlEnd are not equal after LoadFromFile"));
        //}

        //[TestMethod]
        //public void InvalidLoadFromFile()
        //{
        //    DvdCollection loadedDvdCollection;
        //    Exception e;
        //    DvdCollection.LoadFromFile(OutputFolder + @"savedvd.error.xml", out loadedDvdCollection, out e);
        //}

        //private static DvdCollection GetDvd()
        //{
        //    var dvdCol = new DvdCollection();
        //    var newdvd = new dvd {Title = "Matrix", Style = Styles.Action};
        //    newdvd.Actor.Add(new Actor {firstname = "Thomas", lastname = "Anderson"});
        //    dvdCol.Dvds.Add(newdvd);
        //    return dvdCol;
        //}


        private static string GetInputFilePath(string resourceFileName, string fileContent)
        {
            lock (fileLock)
            {
              if (!Directory.Exists(OutputFolder))
              {
                Directory.CreateDirectory(OutputFolder);
              }

              using (var sw = new StreamWriter(Path.Combine(OutputFolder, resourceFileName), false))
                {
                    sw.Write(fileContent);
                }

                return Path.Combine(OutputFolder, resourceFileName);
            }
        }

        private static GeneratorParams GetGeneratorParams(string inputFilePath)
        {
            var generatorParams = new GeneratorParams
                       {
                           InputFilePath = inputFilePath,
                           NameSpace = CodeGenerationNamespace,
                           TargetFramework = TargetFramework.Net20,
                           CollectionObjectType = CollectionType.ObservableCollection,
                           EnableDataBinding = true,
                           GenerateDataContracts = true,
                           GenerateCloneMethod = true,
                           OutputFilePath = GetOutputFilePath(inputFilePath)
                       };
            generatorParams.Miscellaneous.HidePrivateFieldInIde = true;
            generatorParams.Miscellaneous.DisableDebug = true;
            generatorParams.Serialization.Enabled = true;
            return generatorParams;
        }

        /// <summary>
        /// Get output file path
        /// </summary>
        /// <param name="inputFilePath">input file path</param>
        /// <returns></returns>
        static private string GetOutputFilePath(string inputFilePath)
        {
            return Path.ChangeExtension(inputFilePath, ".TestGenerated.cs");
        }

        /// <summary>
        /// Compile file
        /// </summary>
        /// <param name="filePath">CS file path</param>
        /// <returns></returns>
        static private Result<string> CompileCSFile(string filePath)
        {
            var result = new Result<string>(null, true);
            var file = new FileInfo(filePath);
            if (!file.Exists)
            {
                result.Success = false;
                result.Messages.Add(MessageType.Error, "Input file \"{0}\" does not exist", filePath);
            }
            if (result.Success)
            {
                try
                {

                    var outputPath = Path.ChangeExtension(file.FullName, ".dll");
                    result.Entity = outputPath;
                    var csc = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } });
                    var parameters = new CompilerParameters(new[] { "mscorlib.dll", "System.dll", "System.Xml.dll", "WindowsBase.dll", "System.Runtime.Serialization.dll" }, outputPath, true);
                    parameters.GenerateExecutable = false;
                    CompilerResults results = csc.CompileAssemblyFromFile(parameters, filePath);
                    if (results.Errors.HasErrors)
                    {
                        result.Success = false;
                        foreach (CompilerError error in results.Errors)
                        {
                            result.Messages.Add(MessageType.Error, error.ToString());
                        }

                    }

                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Messages.Add(MessageType.Error, "Exception", ex.ToString());
                }
            }

            if (result.Messages.Count > 0)
                Debug.WriteLine(result.Messages.ToString());

            return result;
        }

    }
}