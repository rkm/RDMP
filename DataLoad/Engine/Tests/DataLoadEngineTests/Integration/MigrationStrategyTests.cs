// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using DataLoadEngine.Job;
using DataLoadEngine.Migration;
using DataLoadEngine.Migration.QueryBuilding;
using FAnsi.Connections;
using FAnsi.Discovery;
using NUnit.Framework;
using Tests.Common;
using Moq;

namespace DataLoadEngineTests.Integration
{
    class MigrationStrategyTests : DatabaseTests
    {
        [Test]
        public void OverwriteMigrationStrategy_NoPrimaryKey()
        {
            var from = DiscoveredDatabaseICanCreateRandomTablesIn.CreateTable("Bob",new[] {new DatabaseColumnRequest("Field", "int")});
            var to = DiscoveredDatabaseICanCreateRandomTablesIn.CreateTable("Frank", new[] { new DatabaseColumnRequest("Field", "int") });

            var connection = Mock.Of<IManagedConnection>();
            var job = Mock.Of<IDataLoadJob>();
            var strategy = new OverwriteMigrationStrategy(connection);

            var migrationFieldProcessor = Mock.Of<IMigrationFieldProcessor>();

            var ex = Assert.Throws<Exception>(() => new MigrationColumnSet(from, to, migrationFieldProcessor));
            Assert.AreEqual("There are no primary keys declared in table Bob", ex.Message);
        }
    }

}
