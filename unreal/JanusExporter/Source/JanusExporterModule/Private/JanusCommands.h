#pragma once

#include "JanusExporterStyle.h"

class FJanusCommands : public TCommands<FJanusCommands>
{
public:
	FJanusCommands()
		: TCommands<FJanusCommands>(
			TEXT("JanusExporterExtensions"), // Context name for fast lookup
			NSLOCTEXT("Contents", "Janus", "Janus Exporter"),
			NAME_None, // Parent
			FJanusExporterStyle::Get()->GetStyleSetName() // Icon Style Set
			)
	{
	}

	// TCommand<> interface
	void RegisterCommands() override;
	// End of TCommand<> interface

public:
	//TSharedPtr<FUICommandInfo> TestCommand;
};
