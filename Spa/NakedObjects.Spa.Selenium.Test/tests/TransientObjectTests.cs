﻿// Copyright Naked Objects Group Ltd, 45 Station Road, Henley on Thames, UK, RG9 1AT
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;

namespace NakedObjects.Web.UnitTests.Selenium {

    public abstract class TransientObjectTests : AWTest {

        [TestMethod] 
        public void CreateAndSaveTransientObject()
        {
            GeminiUrl("object?object1=AdventureWorksModel.Person-12043&actions1=open");
            Click(GetObjectAction("Create New Credit Card"));
            SelectDropDownOnField(FieldType.Property, "Card Type", "Vista");
            string number = DateTime.Now.Ticks.ToString(); //pseudo-random string
            var obfuscated  = number.Substring(number.Length - 4).PadLeft(number.Length, '*');
            TypeIntoField(FieldType.Property, "Card Number", number);
            SelectDropDownOnField(FieldType.Property, "Exp Month", "12");
            SelectDropDownOnField(FieldType.Property, "Exp Year", "2020");
            Click(SaveButton()); 
            WaitForView(Pane.Single, PaneType.Object, obfuscated);
        }

        [TestMethod]
        public void SaveAndClose()
        {
            GeminiUrl("object?object1=AdventureWorksModel.Person-12043&actions1=open");
            Click(GetObjectAction("Create New Credit Card"));
            SelectDropDownOnField(FieldType.Property, "Card Type", "Vista");
            string number = DateTime.Now.Ticks.ToString(); //pseudo-random string
            var obfuscated = number.Substring(number.Length - 4).PadLeft(number.Length, '*');
            TypeIntoField(FieldType.Property, "Card Number", number);
            SelectDropDownOnField(FieldType.Property, "Exp Month", "12");
            SelectDropDownOnField(FieldType.Property, "Exp Year", "2020");
            Click(SaveAndCloseButton());
            WaitForView(Pane.Single, PaneType.Object, "Arthur Wilson");
            //But check that credit card was saved nonetheless
            GetObjectAction("List Credit Cards").Click();
            WaitForView(Pane.Single, PaneType.List, "List Credit Cards");
            wait.Until(dr => dr.FindElements(By.CssSelector(".collection table tbody tr td.reference")).Last().Text == obfuscated);
        }

        [TestMethod]
        public void MissingMandatoryFieldsNotified()
        {
            GeminiUrl("object?object1=AdventureWorksModel.Person-12043&actions1=open");
            Click(GetObjectAction("Create New Credit Card"));
            SelectDropDownOnField(FieldType.Property, "Card Type", "Vista");
            SelectDropDownOnField(FieldType.Property, "Exp Year", "2020");
            Click(SaveButton());
            wait.Until(dr => dr.FindElement(
                By.CssSelector("input.cardnumber")).GetAttribute("placeholder") == "REQUIRED * Without spaces");
            wait.Until(dr => dr.FindElement(
                By.CssSelector("select.expmonth option[selected='selected']")).Text =="REQUIRED *");

        }

        [TestMethod]
        public virtual void PropertyDescriptionAndRequiredRenderedAsPlaceholder()
        {
            GeminiUrl("object?object1=AdventureWorksModel.Person-12043&actions1=open");
            Click(GetObjectAction("Create New Credit Card"));
            var name = WaitForCss("input.cardnumber");
            Assert.AreEqual("* Without spaces", name.GetAttribute("placeholder"));
        }

        [TestMethod]
        [Ignore] // SEC 28/10/15 - failing in build
        public void CancelTransientObject()
        {
            GeminiUrl("object?object1=AdventureWorksModel.Person-12043&actions1=open");
            WaitForView(Pane.Single, PaneType.Object, "Arthur Wilson");
            Click(GetObjectAction("Create New Credit Card"));
            Click(GetCancelEditButton());
            WaitForView(Pane.Single, PaneType.Object, "Arthur Wilson");
        }

        [TestMethod, Ignore] //cross-field validation not working
        public void MultiFieldValidation()
        {
            GeminiUrl("object?object1=AdventureWorksModel.Person-12043&actions1=open");
            Click(GetObjectAction("Create New Credit Card"));
            SelectDropDownOnField(FieldType.Property, "Card Type", "Vista");
            TypeIntoField(FieldType.Property, "Card Number", "1111222233334444");
            SelectDropDownOnField(FieldType.Property, "Exp Month", "1");
            SelectDropDownOnField(FieldType.Property, "Exp Year", "2008");
            Click(SaveButton());
            //TODO: Test that dave has failed and message is shown

        }
    }

    #region browsers specific subclasses

    //[TestClass, Ignore]
    public class TransientObjectTestsIe : TransientObjectTests
    {
        [ClassInitialize]
        public new static void InitialiseClass(TestContext context) {
            FilePath(@"drivers.IEDriverServer.exe");
            AWTest.InitialiseClass(context);
        }

        [TestInitialize]
        public virtual void InitializeTest() {
            InitIeDriver();
            Url(BaseUrl);
        }

        [TestCleanup]
        public virtual void CleanupTest() {
            base.CleanUpTest();
        }
    }

    [TestClass]
    public class TransientObjectTestsFirefox : TransientObjectTests
    {
        [ClassInitialize]
        public new static void InitialiseClass(TestContext context) {
            AWTest.InitialiseClass(context);
        }

        [TestInitialize]
        public virtual void InitializeTest() {
            InitFirefoxDriver();
        }

        [TestCleanup]
        public virtual void CleanupTest() {
            base.CleanUpTest();
        }

        protected override void ScrollTo(IWebElement element) {
            string script = string.Format("window.scrollTo({0}, {1});return true;", element.Location.X, element.Location.Y);
            ((IJavaScriptExecutor) br).ExecuteScript(script);
        }
    }

    //[TestClass, Ignore]
    public class TransientObjectTestsChrome : TransientObjectTests
    {
        [ClassInitialize]
        public new static void InitialiseClass(TestContext context) {
            FilePath(@"drivers.chromedriver.exe");
            AWTest.InitialiseClass(context);
        }

        [TestInitialize]
        public virtual void InitializeTest() {
            InitChromeDriver();
        }

        [TestCleanup]
        public virtual void CleanupTest() {
            base.CleanUpTest();
        }
    }

    #endregion
}