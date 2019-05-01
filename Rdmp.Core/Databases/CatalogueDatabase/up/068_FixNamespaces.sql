--Version:2.13.0.10
--Description: Fixes the namespaces of refactored load modules / pipeline components

  UPDATE PipelineComponent SET Class = REPLACE(Class,'LoadModules.Generic.','Rdmp.Core.DataLoad.Modules.')
  UPDATE PipelineComponent SET Class = REPLACE(Class,'DataLoadEngine.','Rdmp.Core.DataLoad.Engine.')
  UPDATE PipelineComponent SET Class = REPLACE(Class,'DataExportLibrary.','Rdmp.Core.DataExport.')
  UPDATE PipelineComponent SET Class = REPLACE(Class,'Rdmp.Core.DataExport.DataRelease.ReleasePipeline.','Rdmp.Core.DataExport.DataRelease.Pipeline.')
  
  UPDATE PipelineComponentArgument SET Type = REPLACE(Type,'LoadModules.Generic.','Rdmp.Core.DataLoad.Modules.')
  UPDATE PipelineComponentArgument SET Type = REPLACE(Type,'DataLoadEngine.','Rdmp.Core.DataLoad.Engine.')
  UPDATE PipelineComponentArgument SET Type = REPLACE(Type,'DataExportLibrary.','Rdmp.Core.DataExport.')
  

  UPDATE ProcessTask SET Path = REPLACE(Path,'LoadModules.Generic.','Rdmp.Core.DataLoad.Modules.')
  
  
  UPDATE ProcessTaskArgument SET Type = REPLACE(Type,'LoadModules.Generic.','Rdmp.Core.DataLoad.Modules.')
  UPDATE ProcessTaskArgument SET Type = REPLACE(Type,'DataLoadEngine.','Rdmp.Core.DataLoad.Engine.')
  UPDATE ProcessTaskArgument SET Type = REPLACE(Type,'DataExportLibrary.','Rdmp.Core.DataExport.')
  