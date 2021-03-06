// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Moq;
using NUnit.Framework;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.CommandLine.Interactive.Picking;
using Tests.Common;

namespace Rdmp.Core.Tests.CommandExecution
{
    class TestExecuteCommandDescribeCommand : UnitTests
    {
        private Mock<IBasicActivateItems> GetMock()
        {
            var mock = new Mock<IBasicActivateItems>();
            mock.Setup(m => m.RepositoryLocator).Returns(RepositoryLocator);
            mock.Setup(m => m.GetDelegates()).Returns(new List<CommandInvokerDelegate>());
            mock.Setup(m => m.Show(It.IsAny<string>()));
            return mock;
        }
        
        /// <summary>
        /// Asserts that the help text <paramref name="forCommand"/> matches your <paramref name="expectedHelp"/> text
        /// </summary>
        /// <param name="expectedHelp"></param>
        /// <param name="forCommand"></param>
        private void AssertHelpIs(string expectedHelp, Type forCommand)
        {
            var mock = GetMock();

            var cmd = new ExecuteCommandDescribeCommand(mock.Object, forCommand);
            Assert.IsFalse(cmd.IsImpossible,cmd.ReasonCommandImpossible);

            cmd.Execute();

            string contents = Regex.Escape(expectedHelp);

            // Called once
            mock.Verify(m => m.Show(It.IsRegex(contents)), Times.Once());
        }

        [Test]
        public void Test_DescribeDeleteCommand()
        {
            AssertHelpIs( @"cmd Delete <deletable> 
PARAMETERS:
deletable	IDeleteable	The object you want to delete",typeof(ExecuteCommandDelete));
        }


        [Test]
        public void Test_ImportTableInfo_CommandHelp()
        {
            AssertHelpIs( 
@"cmd ImportTableInfo <table> <createCatalogue> 
PARAMETERS:
table	DiscoveredTable	The table or view you want to reference from RDMP.  See PickTable for syntax
createCatalogue	Boolean	True to create a Catalogue as well as a TableInfo"
                ,typeof(ExecuteCommandImportTableInfo));

        }
    }
}