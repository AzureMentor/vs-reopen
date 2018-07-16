﻿namespace VSDocumentReopen.Domain
{
	public interface IConfiguration
	{
		string HistoryFileName { get; }
		int MaxAllowedHistoryItems { get; }
		int MaxNumberOfHistoryItemsOnMenu { get; }
		string PackageWorkingDirName { get; }
		string ReopenCommandBinding { get; }
		string ShowMoreCommandBinding { get; }
		string VSTempFolderName { get; }
	}
}