#include "FJanusExporterModulePrivatePCH.h"
#include "JanusExporterWindow.h"

#define LOCTEXT_NAMESPACE "JanusExporter"

void SJanusExporterWindow::Construct(const FArguments& InArgs)
{
	ImportUI = InArgs._ImportUI;
	TSharedPtr<SBox> InspectorBox;
	this->ChildSlot
		[
			SNew(SBox)
			.MaxDesiredHeight(InArgs._MaxWindowWidth)
			.MaxDesiredWidth(InArgs._MaxWindowHeight)
			[
				SNew(SVerticalBox)
				+SVerticalBox::Slot()
				.AutoHeight()
				.Padding(2)
				[
					SAssignNew(InspectorBox, SBox)
					.MaxDesiredHeight(650.0f)
					.WidthOverride(400.0f)
				]
				+ SVerticalBox::Slot()
				.AutoHeight()
				.HAlign(HAlign_Right)
				.Padding(2)
				[
					SNew(SUniformGridPanel)
					.SlotPadding(2)
					+ SUniformGridPanel::Slot(1, 0)
					[
						SNew(SButton)
						.HAlign(HAlign_Center)
						.Text(LOCTEXT("FbxOptionWindow_ImportAll", "Export"))
						.ToolTipText(LOCTEXT("FbxOptionWindow_ImportAll_ToolTip", "Import all files with these same settings"))
						//.IsEnabled(this, &SFbxOptionWindow::CanImport)
						//.OnClicked(this, &SFbxOptionWindow::OnImportAll)
					]
					+ SUniformGridPanel::Slot(2, 0)
					[
						//SAssignNew(ImportButton, SButton)
						SNew(SButton)
						.HAlign(HAlign_Center)
						.Text(LOCTEXT("FbxOptionWindow_Import", "Show In Explorer"))
						//.IsEnabled(this, &SFbxOptionWindow::CanImport)
						//.OnClicked(this, &SFbxOptionWindow::OnImport)
					]
					//+ SUniformGridPanel::Slot(3, 0)
					//[
					//	SNew(SButton)
					//	.HAlign(HAlign_Center)
					//	.Text(LOCTEXT("FbxOptionWindow_Cancel", "Cancel"))
					//	.ToolTipText(LOCTEXT("FbxOptionWindow_Cancel_ToolTip", "Cancels importing this FBX file"))
					//	//.OnClicked(this, &SFbxOptionWindow::OnCancel)
					//]
				]
			]
		];

	FPropertyEditorModule& PropertyEditorModule = FModuleManager::GetModuleChecked<FPropertyEditorModule>("PropertyEditor");
	FDetailsViewArgs DetailsViewArgs;
	DetailsViewArgs.bAllowSearch = false;
	DetailsViewArgs.NameAreaSettings = FDetailsViewArgs::HideNameArea;
	TSharedPtr<IDetailsView> DetailsView = PropertyEditorModule.CreateDetailView(DetailsViewArgs);

	InspectorBox->SetContent(DetailsView->AsShared());
	DetailsView->SetObject(ImportUI);
}

#undef LOCTEXT_NAMESPACE
