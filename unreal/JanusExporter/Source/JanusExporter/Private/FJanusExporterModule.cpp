#include "FJanusExporterModulePrivatePCH.h"
#include "BaseEditorTool.h"
#include "PropertyEditorModule.h"
#include "LevelEditor.h"
#include "BaseEditorToolCustomization.h"
#include "JanusCommands.h"
#include "JanusExporterStyle.h"
#include "Editor/MainFrame/Public/Interfaces/IMainFrameModule.h"
#include "Runtime/Slate/Public/Widgets/SCanvas.h"
#include "JanusExporterWindow.h"
//#include "JanusMaterialUtilities.h"

#define LOCTEXT_NAMESPACE "JanusExporter"

class FJanusExporterModule : public IModuleInterface
{
public:
	// IMoudleInterface interface
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
	// End of IModuleInterface interface

	static void TriggerTool(UClass* ToolClass);
	static void CreateToolListMenu(class FMenuBuilder& MenuBuilder);
	static void OnToolWindowClosed(const TSharedRef<SWindow>& Window, UBaseEditorTool* Instance);

	static void HandleTestCommandExcute();

	static bool HandleTestCommandCanExcute();

	/** Call back for garbage collector, cleans up the RenderTargetPool if CurrentlyRendering is set to false */
	static void OnPreGarbageCollect();

	TSharedPtr<FUICommandList> CommandList;
};

void FJanusExporterModule::OnPreGarbageCollect()
{
	//FJanusMaterialUtilities::ClearRenderTargetPool();
}

void FJanusExporterModule::StartupModule()
{
	//void* Function = &FJanusExporterModule::OnPreGarbageCollect;
	//FCoreUObjectDelegates::PreGarbageCollect.AddRaw(this, Function);

	// Register the details customizations
	{
		FPropertyEditorModule& PropertyModule = FModuleManager::LoadModuleChecked<FPropertyEditorModule>(TEXT("PropertyEditor"));
		PropertyModule.RegisterCustomClassLayout(TEXT("BaseEditorTool"), FOnGetDetailCustomizationInstance::CreateStatic(&FBaseEditorToolCustomization::MakeInstance));
		//PropertyModule.NotifyCustomizationModuleChanged();
	}
	/*TSharedPtr<SWindow> ParentWindow;
	if (FModuleManager::Get().IsModuleLoaded("MainFrame"))
	{
		IMainFrameModule& MainFrame = FModuleManager::LoadModuleChecked<IMainFrameModule>("MainFrame");
		ParentWindow = MainFrame.GetParentWindow();
	}*/

	// Register slate style ovverides
	FJanusExporterStyle::Initialize();

	// Register commands
	FJanusCommands::Register();
	CommandList = MakeShareable(new FUICommandList);

	FLevelEditorModule& LevelEditorModule = FModuleManager::LoadModuleChecked<FLevelEditorModule>(TEXT("LevelEditor"));

	CommandList->Append(LevelEditorModule.GetGlobalLevelEditorActions());

	/*CommandList->MapAction(
		FJanusCommands::Get().TestCommand,
		FExecuteAction::CreateStatic(&FJanusExporterModule::HandleTestCommandExcute),
		FCanExecuteAction::CreateStatic(&FJanusExporterModule::HandleTestCommandCanExcute)
		);*/

	struct Local
	{
		/*static void AddToolbarCommands(FToolBarBuilder& ToolbarBuilder)
		{
			ToolbarBuilder.AddToolBarButton(FJanusCommands::Get().TestCommand);
		}*/

		static void AddMenuCommands(FMenuBuilder& MenuBuilder)
		{
			MenuBuilder.AddSubMenu(LOCTEXT("JanusExporter", "Janus Exporter"),
				LOCTEXT("JanusExporterTooltip", "Janus Exporter tools"),
				FNewMenuDelegate::CreateStatic(&FJanusExporterModule::CreateToolListMenu)
			);
		}
	};

	TSharedRef<FExtender> MenuExtender(new FExtender());
	MenuExtender->AddMenuExtension(
		TEXT("EditMain"),
		EExtensionHook::After,
		CommandList.ToSharedRef(),
		FMenuExtensionDelegate::CreateStatic(&Local::AddMenuCommands));
	LevelEditorModule.GetMenuExtensibilityManager()->AddExtender(MenuExtender);
}

void FJanusExporterModule::ShutdownModule()
{
	//FCoreUObjectDelegates::PreGarbageCollect.RemoveAll(this);
	OnPreGarbageCollect();

	FJanusCommands::Unregister();
	FJanusExporterStyle::Shutdown();
}

void FJanusExporterModule::TriggerTool(UClass* ToolClass)
{
	// Compute centered window position based on max window size, which include when all categories are expanded
	//const float FbxImportWindowWidth = 410.0f;
	//const float FbxImportWindowHeight = 750.0f;
	//const FVector2D FbxImportWindowSize = FVector2D(FbxImportWindowWidth, FbxImportWindowHeight); // Max window size it can get based on current slate

	//FSlateRect WorkAreaRect = FSlateApplicationBase::Get().GetPreferredWorkArea();
	//FVector2D DisplayTopLeft(WorkAreaRect.Left, WorkAreaRect.Top);
	//FVector2D DisplaySize(WorkAreaRect.Right - WorkAreaRect.Left, WorkAreaRect.Bottom - WorkAreaRect.Top);
	//FVector2D WindowPosition = DisplayTopLeft + (DisplaySize - FbxImportWindowSize) / 2.0f;

	//TSharedRef<SWindow> Window = SNew(SWindow)
	//	.Title(LOCTEXT("JanusVR Exporter", "JanusVR Exporter"))
	//	.SizingRule(ESizingRule::Autosized)
	//	.AutoCenter(EAutoCenter::None)
	//	.ScreenPosition(WindowPosition);

	//TSharedPtr<SJanusExporterWindow> JanusWindow;
	//Window->SetContent
	//(
	//	SAssignNew(JanusWindow, SJanusExporterWindow)
	//);

	//// @todo: we can make this slow as showing progress bar later
	//TSharedPtr<SWindow> ParentWindow;

	//if (FModuleManager::Get().IsModuleLoaded("MainFrame"))
	//{
	//	IMainFrameModule& MainFrame = FModuleManager::LoadModuleChecked<IMainFrameModule>("MainFrame");
	//	ParentWindow = MainFrame.GetParentWindow();
	//}

	//FSlateApplication::Get().AddModalWindow(Window, ParentWindow, false);

	//UBaseEditorTool* ToolInstance = NewObject<UBaseEditorTool>(GetTransientPackage(), ToolClass);
	//ToolInstance->AddToRoot();
	//JanusWindow->SetExporterTool((UJanusExporterTool)ToolInstance);

	//return;

	UBaseEditorTool* ToolInstance = NewObject<UBaseEditorTool>(GetTransientPackage(), ToolClass);
	ToolInstance->AddToRoot();

	FPropertyEditorModule& PropertyModule = FModuleManager::LoadModuleChecked<FPropertyEditorModule>("PropertyEditor");

	TArray<UObject*> ObjectsToView;
	ObjectsToView.Add(ToolInstance);

	TSharedRef<SWindow> Window = PropertyModule.CreateFloatingDetailsView(ObjectsToView, /*bIsLockeable=*/ true);
	Window->SetOnWindowClosed(FOnWindowClosed::CreateStatic(&FJanusExporterModule::OnToolWindowClosed, ToolInstance));
}

void FJanusExporterModule::CreateToolListMenu(class FMenuBuilder& MenuBuilder)
{
	for (TObjectIterator<UClass> ClassIt; ClassIt; ++ClassIt)
	{
		UClass* Class = *ClassIt;
		if (!Class->HasAnyClassFlags(CLASS_Deprecated | CLASS_NewerVersionExists | CLASS_Abstract))
		{
			if (Class->IsChildOf(UBaseEditorTool::StaticClass()))
			{
				FString FriendlyName = Class->GetName();
				FText MenuDescription = FText::Format(LOCTEXT("ToolMenuDescription", "{0}"), FText::FromString(FriendlyName));
				FText MenuTooltip = FText::Format(LOCTEXT("ToolMenuTooltip", "Execute the {0} tool"), FText::FromString(FriendlyName));

				FUIAction Action(FExecuteAction::CreateStatic(&FJanusExporterModule::TriggerTool, Class));

				MenuBuilder.AddMenuEntry(
					MenuDescription,
					MenuTooltip,
					FSlateIcon(),
					Action);
			}
		}
	}
}

void FJanusExporterModule::OnToolWindowClosed(const TSharedRef<SWindow>& Window, UBaseEditorTool* Instance)
{
	Instance->RemoveFromRoot();
}

void FJanusExporterModule::HandleTestCommandExcute()
{
	FPlatformMisc::MessageBoxExt(EAppMsgType::Ok, TEXT("Updated!!!"), TEXT("TestCommand"));
}

bool FJanusExporterModule::HandleTestCommandCanExcute()
{
	return true;
}

IMPLEMENT_MODULE(FJanusExporterModule, JanusExporterModule);

#undef LOCTEXT_NAMESPACE