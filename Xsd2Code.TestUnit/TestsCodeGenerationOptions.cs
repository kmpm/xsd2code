using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xsd2Code.Library;

namespace Xsd2Code.TestUnit
{
    [TestClass]
    public class TestsCodeGenerationOptions
    {
        /// <summary>
        /// Represents primitive types by fields and primitive types by System namespace types.
        /// </summary>
        [TestMethod]
        public void WithCodeGenerationOptionNone()
        {
            var generatorParams = new GeneratorParams
            {
                InputXsdString = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
                                    <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
                                        <xs:element name=""shiporder"" type=""orderType"" />
                                        
                                        <xs:complexType name=""orderType"">
                                            <xs:sequence>
                                                <xs:element name=""orderperson"" type=""xs:string""/>
                                                <xs:element name=""item"" maxOccurs=""unbounded"" type=""itemType""/>
                                            </xs:sequence>
                                        </xs:complexType>

                                        <xs:complexType name=""itemType"">
                                            <xs:sequence>
                                                <xs:element name=""title"" type=""xs:string""/>
                                                <xs:element name=""note"" type=""xs:string"" minOccurs=""0""/>
                                                <xs:element name=""quantity"" type=""xs:positiveInteger""/>
                                                <xs:element name=""price"" type=""xs:decimal""/>
                                            </xs:sequence>
                                        </xs:complexType>

                                    </xs:schema>",    
                TargetFramework = TargetFramework.Net20,
                CollectionObjectType = CollectionType.List,
                CodeGenerationOptions = CodeGenerationOptions.None
            };
            generatorParams.Miscellaneous.DisableDebug = true;
            generatorParams.Serialization.Enabled = false;


            var xsdGenResult = Generator.Process(generatorParams);

            var codeProvider = CodeDomProviderFactory.GetProvider(GenerationLanguage.CSharp);
            var resultCode = new StringBuilder();

            using (var outputStream = new StringWriter(resultCode))
                codeProvider.GenerateCodeFromNamespace(xsdGenResult.Entity, outputStream, new CodeGeneratorOptions());


            var expectedCode = @"using System;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Collections;
using System.Xml.Schema;
using System.ComponentModel;
using System.Collections.Generic;


[System.Diagnostics.DebuggerStepThroughAttribute()]
public partial class orderType {
    
    public string orderperson;
    
    public List<itemType> item;
    
    public orderType() {
        this.item = new List<itemType>();
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
public partial class itemType {
    
    public string title;
    
    public string note;
    
    public string quantity;
    
    public decimal price;
}
";

            Assert.AreEqual(expectedCode, resultCode.ToString());
        }



        /// <summary>
        /// Represents primitive types by properties.
        /// </summary>
        [TestMethod]
        public void WithDefaultCodeGenerationOption()
        {
            var generatorParams = new GeneratorParams
            {
                InputXsdString = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
                                    <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
                                        <xs:element name=""shiporder"" type=""orderType"" />
                                        
                                        <xs:complexType name=""orderType"">
                                            <xs:sequence>
                                                <xs:element name=""orderperson"" type=""xs:string""/>
                                                <xs:element name=""item"" maxOccurs=""unbounded"" type=""itemType""/>
                                            </xs:sequence>
                                        </xs:complexType>

                                        <xs:complexType name=""itemType"">
                                            <xs:sequence>
                                                <xs:element name=""title"" type=""xs:string""/>
                                                <xs:element name=""note"" type=""xs:string"" minOccurs=""0""/>
                                                <xs:element name=""quantity"" type=""xs:positiveInteger""/>
                                                <xs:element name=""price"" type=""xs:decimal""/>
                                            </xs:sequence>
                                        </xs:complexType>

                                    </xs:schema>",
                TargetFramework = TargetFramework.Net20,
                CollectionObjectType = CollectionType.List
            };
            generatorParams.Miscellaneous.DisableDebug = true;
            generatorParams.Serialization.Enabled = false;


            var xsdGenResult = Generator.Process(generatorParams);

            var codeProvider = CodeDomProviderFactory.GetProvider(GenerationLanguage.CSharp);
            var resultCode = new StringBuilder();

            using (var outputStream = new StringWriter(resultCode))
                codeProvider.GenerateCodeFromNamespace(xsdGenResult.Entity, outputStream, new CodeGeneratorOptions());


            var expectedCode = @"using System;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Collections;
using System.Xml.Schema;
using System.ComponentModel;
using System.Collections.Generic;


[System.Diagnostics.DebuggerStepThroughAttribute()]
public partial class orderType {
    
    private string orderpersonField;
    
    private List<itemType> itemField;
    
    public orderType() {
        this.itemField = new List<itemType>();
    }
    
    public string orderperson {
        get {
            return this.orderpersonField;
        }
        set {
            this.orderpersonField = value;
        }
    }
    
    public List<itemType> item {
        get {
            return this.itemField;
        }
        set {
            this.itemField = value;
        }
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
public partial class itemType {
    
    private string titleField;
    
    private string noteField;
    
    private string quantityField;
    
    private decimal priceField;
    
    public string title {
        get {
            return this.titleField;
        }
        set {
            this.titleField = value;
        }
    }
    
    public string note {
        get {
            return this.noteField;
        }
        set {
            this.noteField = value;
        }
    }
    
    public string quantity {
        get {
            return this.quantityField;
        }
        set {
            this.quantityField = value;
        }
    }
    
    public decimal price {
        get {
            return this.priceField;
        }
        set {
            this.priceField = value;
        }
    }
}
";

            Assert.AreEqual(expectedCode, resultCode.ToString());
        }



        [TestMethod]
        public void WithCodeGenerationOptionDataBinding()
        {
            var generatorParams = new GeneratorParams
            {
                InputXsdString = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
                                    <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
                                        <xs:element name=""shiporder"" type=""orderType"" />
                                        
                                        <xs:complexType name=""orderType"">
                                            <xs:sequence>
                                                <xs:element name=""orderperson"" type=""xs:string""/>
                                                <xs:element name=""item"" maxOccurs=""unbounded"" type=""itemType""/>
                                            </xs:sequence>
                                        </xs:complexType>

                                        <xs:complexType name=""itemType"">
                                            <xs:sequence>
                                                <xs:element name=""title"" type=""xs:string""/>
                                                <xs:element name=""note"" type=""xs:string"" minOccurs=""0""/>
                                                <xs:element name=""quantity"" type=""xs:positiveInteger""/>
                                                <xs:element name=""price"" type=""xs:decimal""/>
                                            </xs:sequence>
                                        </xs:complexType>

                                    </xs:schema>",
                TargetFramework = TargetFramework.Net20,
                CollectionObjectType = CollectionType.List,
                CodeGenerationOptions = CodeGenerationOptions.EnableDataBinding
            };
            generatorParams.Miscellaneous.DisableDebug = true;
            generatorParams.Serialization.Enabled = false;


            var xsdGenResult = Generator.Process(generatorParams);

            var codeProvider = CodeDomProviderFactory.GetProvider(GenerationLanguage.CSharp);
            var resultCode = new StringBuilder();

            using (var outputStream = new StringWriter(resultCode))
                codeProvider.GenerateCodeFromNamespace(xsdGenResult.Entity, outputStream, new CodeGeneratorOptions());


            var expectedCode = @"using System;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Collections;
using System.Xml.Schema;
using System.ComponentModel;
using System.Collections.Generic;


[System.Diagnostics.DebuggerStepThroughAttribute()]
public partial class orderType : object, System.ComponentModel.INotifyPropertyChanged {
    
    public string orderperson;
    
    public List<itemType> item;
    
    public orderType() {
        this.item = new List<itemType>();
    }
    
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    
    protected void RaisePropertyChanged(string propertyName) {
        System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
        if ((propertyChanged != null)) {
            propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
public partial class itemType : object, System.ComponentModel.INotifyPropertyChanged {
    
    public string title;
    
    public string note;
    
    public string quantity;
    
    public decimal price;
    
    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    
    protected void RaisePropertyChanged(string propertyName) {
        System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
        if ((propertyChanged != null)) {
            propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
";

            Assert.AreEqual(expectedCode, resultCode.ToString());
        }
    }
}

