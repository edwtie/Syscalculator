; *** Inno Setup version 2.0.8+ Dutch messages ***
;
; Translation by Axel Brink <axel@fmf.nl>
;                Timo van Roermund <T.A.v.Roermund@student.tue.nl>
;                Jan Nijtmans <j.nijtmans@chello.nl>
; Last revision: April 24, 2001, by Timo van Roermund

[LangOptions]
LanguageName=Nederlands
LanguageID=$0413
; If the language you are translating to requires special font faces or
; sizes, uncomment any of the following entries and change them accordingly.
;DialogFontName=MS Shell Dlg
;DialogFontSize=8
;DialogFontStandardHeight=13
;TitleFontName=Arial
;TitleFontSize=29
;WelcomeFontName=Arial
;WelcomeFontSize=12
;CopyrightFontName=Arial
;CopyrightFontSize=8

[Messages]

; *** Application titles
SetupAppTitle=Setup
SetupWindowTitle=Setup - %1
UninstallAppTitle=Verwijderen
UninstallAppFullTitle=%1 verwijderen

; *** Icons
DefaultUninstallIconName=%1 verwijderen

; *** Misc. common
InformationTitle=Informatie
ConfirmTitle=Bevestigen
ErrorTitle=Fout

; *** SetupLdr messages
SetupLdrStartupMessage=Hiermee wordt %1 geïnstalleerd. Wilt u doorgaan?
LdrCannotCreateTemp=Kan geen tijdelijk bestand maken. Setup wordt afgesloten
LdrCannotExecTemp=Kan een bestand in de tijdelijke map niet uitvoeren. Setup wordt afgesloten

; *** Startup error messages
LastErrorMessage=%1.%n%nFout %2: %3
SetupFileMissing=Het bestand %1 ontbreekt in de installatiemap. Corrigeer dit probleem of gebruik een andere kopie van het programma.
SetupFileCorrupt=De installatiebestanden zijn beschadigd. Gebruik een andere kopie van het programma.
SetupFileCorruptOrWrongVer=De installatiebestanden zijn beschadigd, of zijn niet compatibel met deze versie van Setup. Corrigeer dit probleem of gebruik een andere kopie van het programma.
NotOnThisPlatform=Dit programma kan niet worden uitgevoerd onder %1.
OnlyOnThisPlatform=Dit programma moet worden uitgevoerd onder %1.
WinVersionTooLowError=Dit programma vereist %1 versie %2 of hoger.
WinVersionTooHighError=Dit programma kan niet worden geïnstalleerd onder %1 versie %2 of hoger.
AdminPrivilegesRequired=U moet aangemeld zijn als een systeembeheerder om dit programma te kunnen installeren.
SetupAppRunningError=Setup heeft vastgesteld dat %1 op dit moment actief is.%n%nSluit alle vensters van dit programma, en klik daarna op OK om verder te gaan, of op Annuleren om Setup af te sluiten.
UninstallAppRunningError=Het verwijderprogramma heeft vastgesteld dat %1 op dit moment actief is.%n%nSluit alle vensters van dit programma, en klik daarna op OK om verder te gaan, of op Annuleren om het verwijderen af te breken.

; *** Misc. errors
ErrorCreatingDir=Setup kan de map "%1" niet maken
ErrorTooManyFilesInDir=Kan geen bestand maken in de map "%1" omdat deze te veel bestanden bevat

; *** Setup common messages
ExitSetupTitle=Setup afsluiten
ExitSetupMessage=Setup is niet voltooid. Als u nu stopt, wordt het programma niet geïnstalleerd.%n%nU kunt Setup later opnieuw uitvoeren om de installatie te voltooien.%n%nSetup afsluiten?
AboutSetupMenuItem=&Over Setup...
AboutSetupTitle=Over Setup
AboutSetupMessage=%1 versie %2%n%3%n%n%1-homepage:%n%4
AboutSetupNote=

; *** Buttons
ButtonBack=< Vo&rige
ButtonNext=&Volgende >
ButtonInstall=&Installeren
ButtonOK=OK
ButtonCancel=Annuleren
ButtonYes=&Ja
ButtonYesToAll=Ja op &alles
ButtonNo=&Nee
ButtonNoToAll=N&ee op alles
ButtonFinish=&Voltooien
ButtonBrowse=&Bladeren...

; *** Common wizard text
ClickNext=Klik op Volgende om verder te gaan.
; , of Annuleren om Setup af te sluiten. --> Doesn't fit!
ClickNextModern=Klik op Volgende om verder te gaan of Annuleren om Setup af te sluiten.
BeveledLabel=

; *** "Welcome" wizard page
WizardWelcome=Welkom
WelcomeLabel1=Welkom bij het installatieprogramma van [name].
WelcomeLabel2=Hiermee wordt [name/ver] geïnstalleerd op deze computer.%n%nU wordt aanbevolen alle actieve programma's af te sluiten voordat u verder gaat. Dit helpt conflicten tijdens de installatie voorkomen.

; *** "Password" wizard page
WizardPassword=Wachtwoord
PasswordLabel1=Deze installatie is beveiligd met een wachtwoord.
passwordLabel3=Voer het wachtwoord in en klik op Volgende om verder te gaan. Wachtwoorden zijn hoofdlettergevoelig.
PasswordEditLabel=&Wachtwoord:
IncorrectPassword=Het ingevoerde wachtwoord is niet correct. Probeer het opnieuw.

; *** "License Agreement" wizard page
WizardLicense=Licentieovereenkomst
LicenseLabel=Lees de volgende belangrijke informatie voordat u verder gaat.
LicenseLabel1=Lees de volgende licentieovereenkomst. Gebruik de schuifbalk of druk op de knop Page Down om de rest van de overeenkomst te zien.
LicenseLabel2=Accepteert u alle voorwaarden van bovenstaande licentieovereenkomst? Als u Nee kiest, wordt Setup afgesloten. Om [name] te installeren, moet u deze overeenkomst accepteren.

; *** "Information" wizard pages
WizardInfoBefore=Informatie
InfoBeforeLabel=Lees de volgende belangrijke informatie voordat u verder gaat.
InfoBeforeClickLabel=Klik op Volgende als u gereed bent om verder te gaan met Setup.
WizardInfoAfter=Informatie
InfoAfterLabel=Lees de volgende belangrijke informatie voordat u verder gaat.
InfoAfterClickLabel=Klik op Volgende als u gereed bent om verder te gaan met Setup.

; *** "Select Destination Directory" wizard page
WizardSelectDir=Kies de doelmap
SelectDirDesc=Waar moet [name] geïnstalleerd worden?
SelectDirLabel=Kies de map waarin u wilt dat Setup [name] installeert en klik vervolgens op Volgende.
DiskSpaceMBLabel=Het programma vereist [mb] MB vrije schijfruimte.
ToUNCPathname=Setup kan niet installeren naar een UNC-padnaam. Als u wilt installeren naar een netwerk, moet u een netwerkverbinding maken.
InvalidPath=U moet een volledig pad met stationsletter invoeren; bijvoorbeeld:%nC:\APP
InvalidDrive=Het geselecteerde station bestaat niet. Kies een ander station.
DiskSpaceWarningTitle=Onvoldoende schijfruimte
DiskSpaceWarning=Setup vereist ten minste %1 kB vrije schijfruimte voor het installeren, maar het geselecteerde station heeft slechts %2 kB beschikbaar.%n%nWilt u toch doorgaan?
BadDirName32=De mapnaam mag geen van de volgende tekens bevatten:%n%n%1
DirExistsTitle=Map bestaat al
DirExists=De map:%n%n%1%n%nbestaat al. Wilt u toch naar die map installeren?
DirDoesntExistTitle=Map bestaat niet
DirDoesntExist=De map:%n%n%1%n%nbestaat niet. Wilt u de map aanmaken?

; *** "Select Components" wizard page
WizardSelectComponents=Selecteer componenten
SelectComponentsDesc=Welke componenten moeten geïnstalleerd worden?
SelectComponentsLabel2=Selecteer de componenten die u wilt installeren. Klik op Volgende als u klaar bent om verder te gaan.
FullInstallation=Volledige installatie
; if possible don't translate 'Compact' as 'Minimal' (I mean 'Minimal' in your language)
CompactInstallation=Compacte installatie
CustomInstallation=Aangepaste installatie
NoUninstallWarningTitle=Component bestaat
NoUninstallWarning=Setup heeft gedetecteerd dat de volgende componenten al geïnstalleerd zijn op uw computer:%n%n%1%n%nAls u de selectie van deze componenten ongedaan maakt, worden ze niet verwijderd.%n%nWilt u toch doorgaan?
ComponentSize1=%1 kB
ComponentSize2=%1 MB
ComponentsDiskSpaceMBLabel=De huidige selectie vereist ten minste [mb] MB vrije schijfruimte.

; *** "Select Additional Tasks" wizard page
WizardSelectTasks=Selecteer extra taken
SelectTasksDesc=Welke extra taken moeten uitgevoerd worden?
SelectTasksLabel2=Selecteer de extra taken die u door Setup wilt laten uitvoeren bij het installeren van [name], en klik vervolgens op Volgende.
ReadyMemoTasks=Extra taken:

; *** "Select Start Menu Folder" wizard page
WizardSelectProgramGroup=Selecteer Startmenu-map
SelectStartMenuFolderDesc=Waar moeten de snelkoppelingen van het programma geplaatst worden?
SelectStartMenuFolderLabel=Kies de map in het menu Start waarin u wilt dat Setup de snelkoppelingen van het programma toevoegt, en klik vervolgens op Volgende.
NoIconsCheck=&Geen snelkoppelingen maken
MustEnterGroupName=U moet een mapnaam invoeren.
BadGroupName=De map mag geen van de volgende tekens bevatten:%n%n%1
NoProgramGroupCheck2=&Geen Startmenu-map maken

; *** "Ready to Install" wizard page
WizardReady=Klaar om te installeren
ReadyLabel1=Setup is nu gereed om te beginnen met het installeren van [name] op deze computer.
ReadyLabel2a=Klik op Installeren om verder te gaan met installeren, of klik op Vorige als u instellingen wilt terugzien of veranderen.
ReadyLabel2b=Klik op Installeren om verder te gaan met installeren.
ReadyMemoDir=Doelmap:
ReadyMemoType=Installatietype:
ReadyMemoComponents=Geselecteerde componenten:
ReadyMemoGroup=Startmenu-map:

; *** "Installing" wizard page
WizardInstalling=Bezig met installeren
InstallingLabel=Setup installeert [name] op uw computer. Een ogenblik geduld...

; *** "Setup Completed" wizard page
WizardFinished=Installatie voltooid
FinishedLabelNoIcons=Setup heeft het installeren van [name] op deze computer voltooid.
FinishedLabel=Setup heeft het installeren van [name] op deze computer voltooid. U kunt het programma uitvoeren met de geïnstalleerde snelkoppelingen.
ClickFinish=Klik op Voltooien om Setup te beëindigen.
FinishedRestartLabel=Setup moet de computer opnieuw opstarten om de installatie van [name] te voltooien. Wilt u nu opnieuw opstarten?
FinishedRestartMessage=Setup moet uw computer opnieuw opstarten om de installatie van [name] te voltooien.%n%nWilt u nu opnieuw opstarten?
ShowReadmeCheck=Ja, ik wil het bestand Leesmij zien
YesRadio=&Ja, start de computer nu opnieuw op
NoRadio=&Nee, ik start de computer later opnieuw op
; used for example as 'Run MyProg.exe'
RunEntryExec=Start %1
; used for example as 'View Readme.txt'
RunEntryShellExec=Bekijk %1

; *** "Setup Needs the Next Disk" stuff
ChangeDiskTitle=Setup heeft de volgende diskette nodig
SelectDirectory=Kies map
SelectDiskLabel2=Voer diskette %1 in en klik op OK.%n%nAls de bestanden op deze diskette in een andere map gevonden kunnen worden dan die hieronder wordt getoond, voer dan het juiste pad in of klik op Bladeren.
PathLabel=&Pad:
; the %3 below is changed to either SDirectoryOld or SDirectoryNew
; depending on whether the user is running Windows 3.x, or 95 or NT 4.0
FileNotInDir2=Kan het bestand "%1" niet vinden in "%2". Voer de juiste diskette in of kies een andere folder.
SelectDirectoryLabel=Geef de lokatie van de volgende diskette.

; *** Installation phase messages
SetupAborted=Setup is niet voltooid.%n%nCorrigeer het probleem en voer Setup opnieuw uit.
EntryAbortRetryIgnore=Klik op Opnieuw om het opnieuw te proberen, op Negeren om toch door te gaan, of op Afbreken om de installatie af te breken.

; *** Installation status messages
StatusCreateDirs=Mappen maken...
StatusExtractFiles=Bestanden uitpakken...
StatusCreateIcons=Pictogrammen maken...
StatusCreateIniEntries=INI-gegevens instellen...
StatusCreateRegistryEntries=Registergegevens instellen...
StatusRegisterFiles=Bestanden registreren...
StatusSavingUninstall=Verwijderingsinformatie opslaan...
StatusRunProgram=Installatie voltooien...

; *** Misc. errors
ErrorInternal=Interne fout %1
ErrorFunctionFailedNoCode=%1 mislukt
ErrorFunctionFailed=%1 mislukt; code %2
ErrorFunctionFailedWithMessage=%1 mislukt; code %2.%n%3
ErrorExecutingProgram=Kan bestand niet uitvoeren:%n%1

; *** DDE errors
ErrorDDEExecute=DDE: Fout bij "execute"-transactie (code: %1)
ErrorDDECommandFailed=DDE: Commando was niet succesvol
ErrorDDERequest=DDE: Fout bij "request"-transactie (code: %1)

; *** Registry errors
ErrorRegOpenKey=Fout bij het openen van registersleutel:%n%1\%2
ErrorRegCreateKey=Fout bij het maken van registersleutel:%n%1\%2
ErrorRegWriteKey=Fout bij het schrijven naar registersleutel:%n%1\%2

; *** INI errors
ErrorIniEntry=Fout bij het maken van een INI-instelling in bestand "%1".

; *** File copying errors
FileAbortRetryIgnore=Klik op Opnieuw om het opnieuw te proberen, op Negeren om dit bestand over te slaan (niet aanbevolen), of op Afbreken om de installatie af te breken.
FileAbortRetryIgnore2=Klik op Opnieuw om het opnieuw te proberen, op Negeren om toch door te gaan (niet aanbevolen), of op Afbreken om de installatie af te breken.
SourceIsCorrupted=Het bronbestand is beschadigd
SourceDoesntExist=Het bronbestand "%1" bestaat niet
ExistingFileReadOnly=Het bestaande bestand is gemarkeerd als alleen-lezen.%n%nKlik op Opnieuw om het kenmerk alleen-lezen te verwijderen en opnieuw te proberen, op Negeren om dit bestand over te slaan, of op Afbreken om de installatie af te breken.
ErrorReadingExistingDest=Er is een fout opgetreden bij het lezen van het bestaande bestand:
FileExists=Het bestand bestaat al.%n%nWilt u dat Setup het overschrijft?
ExistingFileNewer=Het bestaande bestand is nieuwer dan het bestand dat Setup probeert te installeren. U wordt aanbevolen het bestaande bestand te behouden.%n%nWilt u het bestaande bestand behouden?
ErrorChangingAttr=Er is een fout opgetreden bij het wijzigen van de kenmerken van het bestaande bestand:
ErrorCreatingTemp=Er is een fout opgetreden bij het maken van een bestand in de doelmap:
ErrorReadingSource=Er is een fout opgetreden bij het lezen van het bronbestand:
ErrorCopying=Er is een fout opgetreden bij het kopiëren van een bestand:
ErrorReplacingExistingFile=Er is een fout opgetreden bij het vervangen van het bestaande bestand:
ErrorRestartReplace=RestartReplace mislukt:
ErrorRenamingTemp=Er is een fout opgetreden bij het hernoemen van een bestand in de doelmap:
ErrorRegisterServer=Kan de DLL/OCX niet registreren: %1
ErrorRegisterServerMissingExport=DllRegisterServer export niet gevonden
ErrorRegisterTypeLib=Kan de type library niet registreren: %1

; *** Post-installation errors
ErrorOpeningReadme=Er is een fout opgetreden bij het openen van het Leesmij-bestand.
ErrorRestartingComputer=Setup kan de computer niet opnieuw opstarten. Doe dit handmatig.

; *** Uninstaller messages
UninstallNotFound=Bestand "%1" bestaat niet. Kan het programma niet verwijderen.
UninstallUnsupportedVer=Het installatie-logbestand "%1" heeft een formaat dat niet herkend wordt door deze versie van het verwijderprogramma. Kan het programma niet verwijderen
UninstallUnknownEntry=Er is een onbekend gegeven (%1) aangetroffen in het installatie-logbestand
ConfirmUninstall=Weet u zeker dat u %1 en alle bijbehorende componenten wilt verwijderen?
OnlyAdminCanUninstall=Deze installatie kan alleen worden verwijderd door een gebruiker met administratieve rechten.
UninstallStatusLabel=%1 wordt verwijderd van uw computer. Een ogenblik geduld.
UninstalledAll=%1 is met succes van deze computer verwijderd.
UninstalledMost=Het verwijderen van %1 is voltooid.%n%nEnkele elementen konden niet verwijderd worden. Deze kunnen handmatig verwijderd worden.
UninstallDataCorrupted="%1" bestand is beschadigd. Kan verwijderen niet voltooien

; *** Uninstallation phase messages
ConfirmDeleteSharedFileTitle=Gedeeld bestand verwijderen?
ConfirmDeleteSharedFile2=Het systeem geeft aan dat het volgende gedeelde bestand niet langer gebruikt wordt door enig programma. Wilt u dat dit gedeelde bestand verwijderd wordt?%n%nAls dit bestand toch nog gebruikt wordt door een programma en het verwijderd wordt, werkt dat programma misschien niet meer correct. Als u het niet zeker weet, kies dan Nee. Bewaren van het bestand op dit systeem is niet schadelijk.
SharedFileNameLabel=Bestandsnaam:
SharedFileLocationLabel=Lokatie:
WizardUninstalling=Verwijderingsstatus
StatusUninstalling=Verwijderen van %1...
