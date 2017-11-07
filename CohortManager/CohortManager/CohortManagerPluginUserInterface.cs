﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.CommandExecution.AtomicCommands;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Aggregation;
using CatalogueLibrary.Data.Cohort;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using CatalogueManager.PluginChildProvision;
using CohortManager.CommandExecution.AtomicCommands;
using CohortManager.SubComponents.Graphs;
using CohortManagerLibrary.QueryBuilding;
using DataExportManager.Collections.Nodes;
using ReusableUIComponents.Icons.IconProvision;

namespace CohortManager
{
    public class CohortManagerPluginUserInterface:PluginUserInterface
    {
        public CohortManagerPluginUserInterface(IActivateItems itemActivator) : base(itemActivator)
        {
        }

        public override object[] GetChildren(object model)
        {
            return null;
        }

        public override ToolStripMenuItem[] GetAdditionalRightClickMenuItems(DatabaseEntity databaseEntity)
        {
            #region Aggregate Graphs (Generate graphs in which we combine a cohort aggregate (or container) with an aggregate graph)

            var aggregate = databaseEntity as AggregateConfiguration;
            var aggregateContainer = databaseEntity as CohortAggregateContainer;

            //if it is a cohort aggregate
            if (aggregate != null && aggregate.IsCohortIdentificationAggregate)
            {
                //with a cic (it really should do!)
                var cic = aggregate.GetCohortIdentificationConfigurationIfAny();
                
                if (cic != null)
                {
                    //find other non cohort aggregates (graphs) 
                    var graphsAvailableInCatalogue = CohortSummaryQueryBuilder.GetAllCompatibleSummariesForCohort(aggregate);

                    //and offer graph generation for the cohort subsets
                    var matchRecords = new ToolStripMenuItem("Graph Matching Records Only",ItemActivator.CoreIconProvider.GetImage(RDMPConcept.AggregateGraph));
                    var matchIdentifiers = new ToolStripMenuItem("Graph All Records For Matching Patients",ItemActivator.CoreIconProvider.GetImage(RDMPConcept.AggregateGraph));

                    matchRecords.Enabled = graphsAvailableInCatalogue.Any();
                    matchIdentifiers.Enabled = graphsAvailableInCatalogue.Any() && cic.QueryCachingServer_ID != null;

                    foreach (AggregateConfiguration graph in graphsAvailableInCatalogue)
                    {
                        //records in
                        matchRecords.DropDownItems.Add(
                            GetMenuItem(
                            new ExecuteCommandViewCohortAggregateGraph(ItemActivator,new CohortSummaryAggregateGraphObjectCollection(aggregate, graph,CohortSummaryAdjustment.WhereRecordsIn))
                            ));

                        //extraction identifiers in
                        matchIdentifiers.DropDownItems.Add(
                            GetMenuItem(
                            new ExecuteCommandViewCohortAggregateGraph(ItemActivator, new CohortSummaryAggregateGraphObjectCollection(aggregate, graph, CohortSummaryAdjustment.WhereExtractionIdentifiersIn))
                            ));
                    }

                    return new[] {matchRecords, matchIdentifiers};
                }
            }

            //if it's an aggregate container e.g. EXCEPT/UNION/INTERSECT
            if (aggregateContainer != null)
            {
                var cic = aggregateContainer.GetCohortIdentificationConfiguration();

                //this requires cache to exist (and be populated for the container)
                if (cic != null && cic.QueryCachingServer_ID != null)
                {
                    var matchIdentifiers = new ToolStripMenuItem("Graph All Records For Matching Patients", ItemActivator.CoreIconProvider.GetImage(RDMPConcept.AggregateGraph));

                    var availableGraphs = ItemActivator.CoreChildProvider.AllAggregateConfigurations.Where(g => !g.IsCohortIdentificationAggregate).ToArray();
                    var allCatalogues = ItemActivator.CoreChildProvider.AllCatalogues;

                    if (availableGraphs.Any())
                    {

                        foreach (var cata in allCatalogues.OrderBy(c => c.Name))
                        {
                            var cataGraphs = availableGraphs.Where(g => g.Catalogue_ID == cata.ID).ToArray();
                            
                            //if there are no graphs belonging to the Catalogue skip it
                            if(!cataGraphs.Any())
                                continue;

                            //otherwise create a subheading for it
                            var catalogueSubheading = new ToolStripMenuItem(cata.Name, CatalogueIcons.Catalogue);

                            //add graph for each in the Catalogue
                            foreach (var graph in cataGraphs)
                            {
                                catalogueSubheading.DropDownItems.Add(
                                    GetMenuItem(
                                        new ExecuteCommandViewCohortAggregateGraph(ItemActivator, new CohortSummaryAggregateGraphObjectCollection(aggregateContainer, graph))
                                        ));
                            }

                            matchIdentifiers.DropDownItems.Add(catalogueSubheading);
                        }

                        return new [] { matchIdentifiers };
                    }
                }
            }
            #endregion

            return null;
        }

        public override Bitmap GetImage(object concept, OverlayKind kind = OverlayKind.None)
        {
            return null;
        }

        public override ToolStripMenuItem[] GetAdditionalRightClickMenuItems(object o)
        {
            var cicAssocNode = o as ProjectCohortIdentificationConfigurationAssociationsNode;

            if (cicAssocNode != null)
                return
                    GetMenuArray(
                        new ExecuteCommandCreateNewCohortIdentificationConfiguration(ItemActivator).SetTarget(cicAssocNode.Project)
                        );

            return base.GetAdditionalRightClickMenuItems(o);
        }
    }
}