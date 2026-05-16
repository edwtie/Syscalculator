from pathlib import Path
from html import escape


ROOT = Path(__file__).resolve().parents[1] / "help"
ENCODING = "cp1252"

ASSETS = [
    "logo_phpBB.gif",
    "Logo_phpBB_med.gif",
    "syscal_1.png",
    "syscal_2.png",
    "Syscal_3.png",
    "Syscal_4.png",
    "triconvert_3.jpg",
    "warning_triangle.png",
]

STYLE = """
body { font-family: Tahoma, Arial, sans-serif; font-size: 10pt; margin: 18px; color: #202020; background: #ffffff; }
h1 { font-size: 18pt; margin: 0 0 10px 0; }
p { margin: 0 0 10px 0; line-height: 1.35; }
ul, ol { margin-top: 6px; margin-bottom: 12px; }
li { margin: 4px 0; }
img { max-width: 100%; height: auto; border: 0; }
.nav { border-bottom: 1px solid #c0c0c0; margin-bottom: 12px; padding-bottom: 8px; }
.note { border-top: 1px solid #c0c0c0; margin-top: 18px; padding-top: 10px; color: #505050; }
.warning { border: 1px solid #b07800; background: #fff7d6; padding: 8px; margin: 12px 0; }
.warning img { vertical-align: middle; margin-right: 8px; }
.warning-title { font-weight: bold; }
""".strip()


def html_page(title, lang, body):
    return f"""<!DOCTYPE html>
<html>
<head>
<meta http-equiv="Content-Language" content="{lang}">
<meta http-equiv="Content-Type" content="text/html; charset=windows-1252">
<title>{title}</title>
<style>
{STYLE}
</style>
</head>
<body>
{body.strip()}
</body>
</html>
"""


HELP = {
    "en": {
        "lang": "en-us",
        "home": "index_en.htm",
        "home_name": "Start",
        "title": "Syscalculator 1.74 Help - English",
        "intro": "Syscalculator 1.74 keeps the classic Visual Basic 6 converter available as a legacy maintenance release.",
        "select": "Choose a topic from the contents pane or from the list below.",
        "topics": [
            ("Convert a Value", "help_en_convert.htm", """<h1>Convert a Value</h1>
<div class="nav"><a href="index_en.htm">English help</a></div>
<p>Use the main window for normal converter work. The catalog decides which converters appear in the list.</p>
<ol><li>Choose a converter from the list.</li><li>Type the source value in the first field.</li><li>Read the calculated result in the second field.</li><li>Use reverse direction when you want to calculate back through the same converter.</li></ol>
<p>The result updates through the old NOD converter rules. If a converter was added or repaired while the program is running, reload the catalog or restart Syscalculator.</p>
<p><a href="Syscal_3.png"><img src="Syscal_3.png" width="299" height="229" alt="Main converter window"></a></p>"""),
            ("Fields, Editing and Clipboard", "help_en_fields.htm", """<h1>Fields, Editing and Clipboard</h1>
<div class="nav"><a href="index_en.htm">English help</a></div>
<p>The edit commands work on the active value field. Select text first when you only want to change part of a value.</p>
<ul><li><b>Copy</b> copies the selected text.</li><li><b>Cut</b> copies and removes the selected text.</li><li><b>Paste</b> inserts clipboard text into the active field.</li><li><b>Delete</b> removes the selected text.</li><li><b>Select All</b> selects the complete value in the active field.</li></ul>
<p>Empty fields are ignored by the dedicated input/output copy commands, so an accidental empty copy does not overwrite useful clipboard content.</p>"""),
            ("WizardExpress", "help_en_wizardexpress.htm", """<h1>WizardExpress</h1>
<div class="nav"><a href="index_en.htm">English help</a></div>
<p>WizardExpress converts values copied to the clipboard. It is intended for older Office and Word versions, classic spreadsheet workflows and plain text.</p>
<ol><li>Copy the values from the source application.</li><li>Open WizardExpress.</li><li>Run the conversion.</li><li>Paste the converted values back into the source application.</li></ol>
<p><b>Compatibility note:</b> LibreOffice has been tested and works ok. Microsoft 365 / Office 365 is known not to work reliably with this old VB6 clipboard integration.</p>
<p><a href="syscal_1.png"><img src="syscal_1.png" width="969" height="593" alt="WizardExpress source values"></a></p>
<p><img src="syscal_2.png" width="947" height="631" alt="WizardExpress converted values"></p>"""),
            ("Calculator", "help_en_calculator.htm", """<h1>Calculator</h1>
<div class="nav"><a href="index_en.htm">English help</a></div>
<p>The calculator is a small classic helper for quick arithmetic beside the converter window.</p>
<ul><li>Use it for temporary calculations before entering a converter value.</li><li>Copy the final number back into the converter field when needed.</li><li>Keep number formatting simple when moving values between calculator and converter.</li></ul>
<p><a href="Syscal_3.png"><img src="Syscal_3.png" width="299" height="229" alt="Calculator and converter window"></a></p>"""),
            ("Configuration", "help_en_configuration.htm", """<h1>Configuration</h1>
<div class="nav"><a href="index_en.htm">English help</a></div>
<p>The options window controls language, default converter and catalog settings.</p>
<ul><li>Choose the language file used by Syscalculator.</li><li>Set the default converter that is selected on startup.</li><li>Add, remove or reorder catalog entries.</li><li>Apply changes before closing the options window.</li></ul>
<p>On modern Windows, configuration is copied to the user data folder where possible. This avoids write permission problems in the install folder.</p>
<p><img src="Syscal_4.png" width="508" height="356" alt="Configuration window"></p>"""),
            ("Window, Tray and Decimals", "help_en_window_options.htm", """<h1>Window, Tray and Decimals</h1>
<div class="nav"><a href="index_en.htm">English help</a></div>
<p>These options control how Syscalculator behaves during daily use.</p>
<ul><li><b>Always on Top</b> keeps the main window above other windows.</li><li><b>Tray</b> allows Syscalculator to keep running from the notification area.</li><li><b>Start with Windows</b> starts Syscalculator hidden in the tray.</li><li><b>Decimals</b> controls how many decimal places are shown in numeric results.</li><li><b>Digit group</b> displays grouped numbers where the converter supports that format.</li></ul>"""),
            ("NOD Files and Catalog", "help_en_nod_catalog.htm", """<h1>NOD Files and Catalog</h1>
<div class="nav"><a href="index_en.htm">English help</a></div>
<p>Syscalculator converters are stored as classic NOD files. The catalog lists those files and gives them names in the main converter list.</p>
<ul><li>Keep NOD files in the expected converter folders.</li><li>Add catalog entries when a new converter should appear in the list.</li><li>Restart Syscalculator or reload the catalog after changing converter files.</li></ul>
<p>The 1.74 release includes the classic distance, mass, pressure, temperature, volume, text and euro legacy currency converters.</p>
<p><img src="triconvert_3.jpg" width="761" height="505" alt="Tri-conversion example"></p>"""),
            ("Limitations and Support", "help_en_support.htm", """<h1>Limitations and Support</h1>
<div class="nav"><a href="index_en.htm">English help</a></div>
<p>Syscalculator 1.74 is a legacy VB6 maintenance release. It keeps historical compatibility available, but it is not a modern .NET application.</p>
<div class="warning"><img src="warning_triangle.png" width="32" height="32" alt="!"><span class="warning-title">Important limitation:</span> this old VB6 version depends on Windows HTML Help, the VB6 runtime and classic clipboard behavior.</div>
<ul><li>The Visual Basic 6 Runtime is required.</li><li>Windows 11 was tested for this maintenance release.</li><li>Older Windows versions are historical targets and are not always retested.</li><li>Microsoft 365 / Office 365 clipboard behavior can break WizardExpress workflows.</li></ul>
<p>This release is intended for users who still need the classic Syscalculator workflow.</p>"""),
        ],
    },
    "nl": {
        "lang": "nl",
        "home": "index_nl.htm",
        "home_name": "Start",
        "title": "Syscalculator 1.74 Help - Nederlands",
        "intro": "Syscalculator 1.74 houdt de klassieke Visual Basic 6 converter beschikbaar als legacy-onderhoudsrelease.",
        "select": "Kies een onderwerp in de inhoudsopgave of uit de lijst hieronder.",
        "topics": [
            ("Waarde Converteren", "help_nl_converteren.htm", """<h1>Waarde Converteren</h1>
<div class="nav"><a href="index_nl.htm">Nederlandse help</a></div>
<p>Gebruik het hoofdvenster voor normaal converteren. De catalogus bepaalt welke converters in de lijst staan.</p>
<ol><li>Kies een converter uit de lijst.</li><li>Typ de bronwaarde in het eerste veld.</li><li>Lees het berekende resultaat in het tweede veld.</li><li>Gebruik richting omkeren wanneer je via dezelfde converter terug wilt rekenen.</li></ol>
<p>Als een converter is toegevoegd of gerepareerd terwijl het programma draait, herlaad de catalogus of start Syscalculator opnieuw.</p>
<p><a href="Syscal_3.png"><img src="Syscal_3.png" width="299" height="229" alt="Hoofdvenster"></a></p>"""),
            ("Velden, Bewerken en Klembord", "help_nl_velden.htm", """<h1>Velden, Bewerken en Klembord</h1>
<div class="nav"><a href="index_nl.htm">Nederlandse help</a></div>
<p>De bewerkcommando's werken op het actieve waardeveld. Selecteer eerst tekst wanneer je maar een deel van een waarde wilt aanpassen.</p>
<ul><li><b>Kopieren</b> kopieert de geselecteerde tekst.</li><li><b>Knippen</b> kopieert en verwijdert de geselecteerde tekst.</li><li><b>Plakken</b> zet klembordtekst in het actieve veld.</li><li><b>Verwijderen</b> verwijdert de geselecteerde tekst.</li><li><b>Alles selecteren</b> selecteert de volledige waarde in het actieve veld.</li></ul>
<p>Lege velden worden genegeerd door de aparte invoer/uitvoer-kopieercommando's.</p>"""),
            ("WizardExpress", "help_nl_wizardexpress.htm", """<h1>WizardExpress</h1>
<div class="nav"><a href="index_nl.htm">Nederlandse help</a></div>
<p>WizardExpress converteert waarden die naar het klembord zijn gekopieerd. Het is bedoeld voor oudere Office- en Word-versies, klassieke spreadsheet-workflows en platte tekst.</p>
<ol><li>Kopieer de waarden uit de bronapplicatie.</li><li>Open WizardExpress.</li><li>Voer de conversie uit.</li><li>Plak de geconverteerde waarden terug in de bronapplicatie.</li></ol>
<p><b>Compatibiliteitsnotitie:</b> LibreOffice is getest en werkt ok. Microsoft 365 / Office 365 werkt niet betrouwbaar met deze oude VB6-klembordkoppeling.</p>
<p><a href="syscal_1.png"><img src="syscal_1.png" width="969" height="593" alt="WizardExpress bronwaarden"></a></p>
<p><img src="syscal_2.png" width="947" height="631" alt="WizardExpress geconverteerde waarden"></p>"""),
            ("Rekenmachine", "help_nl_rekenmachine.htm", """<h1>Rekenmachine</h1>
<div class="nav"><a href="index_nl.htm">Nederlandse help</a></div>
<p>De rekenmachine is een kleine klassieke hulp voor snelle berekeningen naast het convertervenster.</p>
<ul><li>Gebruik hem voor tijdelijke berekeningen voordat je een converterwaarde invult.</li><li>Kopieer het eindgetal terug naar het converterveld wanneer dat nodig is.</li><li>Houd de getalnotatie eenvoudig wanneer je waarden tussen rekenmachine en converter verplaatst.</li></ul>
<p><a href="Syscal_3.png"><img src="Syscal_3.png" width="299" height="229" alt="Rekenmachine en converter"></a></p>"""),
            ("Configuratie", "help_nl_configuratie.htm", """<h1>Configuratie</h1>
<div class="nav"><a href="index_nl.htm">Nederlandse help</a></div>
<p>Het optiescherm regelt taal, standaardconverter en catalogusinstellingen.</p>
<ul><li>Kies het taalbestand dat Syscalculator gebruikt.</li><li>Stel de standaardconverter in.</li><li>Voeg catalogusregels toe, verwijder ze of wijzig de volgorde.</li><li>Pas wijzigingen toe voordat je het optiescherm sluit.</li></ul>
<p>Op moderne Windows wordt configuratie waar mogelijk naar de gebruikersdatamap gekopieerd.</p>
<p><img src="Syscal_4.png" width="508" height="356" alt="Configuratiescherm"></p>"""),
            ("Venster, Tray en Decimalen", "help_nl_venster_opties.htm", """<h1>Venster, Tray en Decimalen</h1>
<div class="nav"><a href="index_nl.htm">Nederlandse help</a></div>
<p>Deze opties bepalen hoe Syscalculator zich gedraagt tijdens dagelijks gebruik.</p>
<ul><li><b>Altijd zichtbaar</b> houdt het hoofdvenster boven andere vensters.</li><li><b>Tray</b> laat Syscalculator vanuit het systeemvak actief blijven.</li><li><b>Starten met Windows</b> start Syscalculator verborgen in de tray.</li><li><b>Decimalen</b> bepaalt hoeveel cijfers achter de komma worden getoond.</li><li><b>Getalgroep</b> toont gegroepeerde getallen wanneer de converter dat ondersteunt.</li></ul>"""),
            ("NOD-bestanden en Catalogus", "help_nl_nod_catalogus.htm", """<h1>NOD-bestanden en Catalogus</h1>
<div class="nav"><a href="index_nl.htm">Nederlandse help</a></div>
<p>Syscalculator-converters worden opgeslagen als klassieke NOD-bestanden. De catalogus geeft die bestanden een naam in de converterlijst.</p>
<ul><li>Bewaar NOD-bestanden in de verwachte convertermappen.</li><li>Voeg catalogusregels toe wanneer een nieuwe converter in de lijst moet verschijnen.</li><li>Start Syscalculator opnieuw of herlaad de catalogus na wijzigingen.</li></ul>
<p>De 1.74-release bevat de klassieke converters voor afstand, massa, druk, temperatuur, volume, tekst en historische euromunten.</p>
<p><img src="triconvert_3.jpg" width="761" height="505" alt="Tri-conversie voorbeeld"></p>"""),
            ("Beperkingen en Support", "help_nl_support.htm", """<h1>Beperkingen en Support</h1>
<div class="nav"><a href="index_nl.htm">Nederlandse help</a></div>
<p>Syscalculator 1.74 is een legacy VB6-onderhoudsrelease. Hij houdt historische compatibiliteit beschikbaar, maar is geen moderne .NET-applicatie.</p>
<div class="warning"><img src="warning_triangle.png" width="32" height="32" alt="!"><span class="warning-title">Belangrijke beperking:</span> deze oude VB6-versie hangt af van Windows HTML Help, de VB6-runtime en klassiek klembordgedrag.</div>
<ul><li>De Visual Basic 6 Runtime is verplicht.</li><li>Windows 11 is getest voor deze onderhoudsrelease.</li><li>Oudere Windows-versies zijn historische doelen en worden niet altijd opnieuw getest.</li><li>Microsoft 365 / Office 365 kan WizardExpress-workflows met het klembord verstoren.</li></ul>
<p>Deze release is bedoeld voor gebruikers die de klassieke Syscalculator-werkwijze nog nodig hebben.</p>"""),
        ],
    },
    "es": {
        "lang": "es",
        "home": "index_es.htm",
        "home_name": "Start",
        "title": "Syscalculator 1.74 Help - Espanol",
        "intro": "Syscalculator 1.74 conserva el conversor clasico de Visual Basic 6 como version legacy de mantenimiento.",
        "select": "Elija un tema en el panel de contenido o en la lista siguiente.",
        "topics": [
            ("Convertir un Valor", "help_es_convertir.htm", """<h1>Convertir un Valor</h1>
<div class="nav"><a href="index_es.htm">Ayuda en espanol</a></div>
<p>Use la ventana principal para el trabajo normal de conversion. El catalogo decide que conversores aparecen en la lista.</p>
<ol><li>Elija un conversor de la lista.</li><li>Escriba el valor de origen en el primer campo.</li><li>Lea el resultado calculado en el segundo campo.</li><li>Use la direccion inversa para calcular en sentido contrario.</li></ol>
<p>Si anade o repara un conversor mientras el programa esta abierto, recargue el catalogo o reinicie Syscalculator.</p>
<p><a href="Syscal_3.png"><img src="Syscal_3.png" width="299" height="229" alt="Ventana principal"></a></p>"""),
            ("Campos, Edicion y Portapapeles", "help_es_campos.htm", """<h1>Campos, Edicion y Portapapeles</h1>
<div class="nav"><a href="index_es.htm">Ayuda en espanol</a></div>
<p>Los comandos de edicion trabajan sobre el campo activo. Seleccione texto primero si solo quiere cambiar parte de un valor.</p>
<ul><li><b>Copiar</b> copia el texto seleccionado.</li><li><b>Cortar</b> copia y elimina el texto seleccionado.</li><li><b>Pegar</b> inserta texto del portapapeles en el campo activo.</li><li><b>Eliminar</b> borra el texto seleccionado.</li><li><b>Seleccionar todo</b> selecciona todo el valor del campo activo.</li></ul>
<p>Los campos vacios se ignoran en los comandos especiales de copiar entrada/salida.</p>"""),
            ("WizardExpress", "help_es_wizardexpress.htm", """<h1>WizardExpress</h1>
<div class="nav"><a href="index_es.htm">Ayuda en espanol</a></div>
<p>WizardExpress convierte valores copiados al portapapeles. Esta pensado para versiones antiguas de Office y Word, hojas de calculo clasicas y texto simple.</p>
<ol><li>Copie los valores desde la aplicacion de origen.</li><li>Abra WizardExpress.</li><li>Ejecute la conversion.</li><li>Pegue los valores convertidos de nuevo en la aplicacion.</li></ol>
<p><b>Nota de compatibilidad:</b> LibreOffice se ha probado y funciona correctamente. Microsoft 365 / Office 365 no funciona de forma fiable con esta integracion VB6 antigua del portapapeles.</p>
<p><a href="syscal_1.png"><img src="syscal_1.png" width="969" height="593" alt="WizardExpress valores de origen"></a></p>
<p><img src="syscal_2.png" width="947" height="631" alt="WizardExpress valores convertidos"></p>"""),
            ("Calculadora", "help_es_calculadora.htm", """<h1>Calculadora</h1>
<div class="nav"><a href="index_es.htm">Ayuda en espanol</a></div>
<p>La calculadora es una pequena ayuda clasica para calculos rapidos junto a la ventana del conversor.</p>
<ul><li>Usela para calculos temporales antes de introducir un valor.</li><li>Copie el numero final al campo del conversor cuando sea necesario.</li><li>Mantenga simple el formato numerico al mover valores entre calculadora y conversor.</li></ul>
<p><a href="Syscal_3.png"><img src="Syscal_3.png" width="299" height="229" alt="Calculadora y conversor"></a></p>"""),
            ("Configuracion", "help_es_configuracion.htm", """<h1>Configuracion</h1>
<div class="nav"><a href="index_es.htm">Ayuda en espanol</a></div>
<p>La ventana de opciones controla idioma, conversor predeterminado y catalogo.</p>
<ul><li>Elija el archivo de idioma usado por Syscalculator.</li><li>Defina el conversor predeterminado para el inicio.</li><li>Anada, elimine o reordene entradas del catalogo.</li><li>Aplique los cambios antes de cerrar la ventana de opciones.</li></ul>
<p>En Windows moderno, la configuracion se copia a la carpeta de datos del usuario cuando es posible.</p>
<p><img src="Syscal_4.png" width="508" height="356" alt="Configuracion"></p>"""),
            ("Ventana, Bandeja y Decimales", "help_es_ventana_opciones.htm", """<h1>Ventana, Bandeja y Decimales</h1>
<div class="nav"><a href="index_es.htm">Ayuda en espanol</a></div>
<p>Estas opciones controlan el comportamiento diario de Syscalculator.</p>
<ul><li><b>Siempre visible</b> mantiene la ventana principal sobre otras ventanas.</li><li><b>Bandeja</b> permite que Syscalculator siga activo en el area de notificacion.</li><li><b>Iniciar con Windows</b> inicia Syscalculator oculto en la bandeja.</li><li><b>Decimales</b> controla cuantos decimales se muestran.</li><li><b>Grupo de digitos</b> muestra numeros agrupados cuando el conversor lo admite.</li></ul>"""),
            ("Archivos NOD y Catalogo", "help_es_nod_catalogo.htm", """<h1>Archivos NOD y Catalogo</h1>
<div class="nav"><a href="index_es.htm">Ayuda en espanol</a></div>
<p>Los conversores de Syscalculator se guardan como archivos NOD clasicos. El catalogo les da nombre dentro de la lista principal.</p>
<ul><li>Mantenga los archivos NOD en las carpetas de conversores esperadas.</li><li>Anada entradas al catalogo cuando un conversor nuevo deba aparecer en la lista.</li><li>Reinicie Syscalculator o recargue el catalogo despues de cambiar archivos de conversor.</li></ul>
<p>La version 1.74 incluye conversores clasicos de distancia, masa, presion, temperatura, volumen, texto y monedas historicas del euro.</p>
<p><img src="triconvert_3.jpg" width="761" height="505" alt="Ejemplo de tri-conversion"></p>"""),
            ("Limitaciones y Soporte", "help_es_soporte.htm", """<h1>Limitaciones y Soporte</h1>
<div class="nav"><a href="index_es.htm">Ayuda en espanol</a></div>
<p>Syscalculator 1.74 es una version de mantenimiento legacy en VB6. Conserva compatibilidad historica, pero no es una aplicacion moderna .NET.</p>
<div class="warning"><img src="warning_triangle.png" width="32" height="32" alt="!"><span class="warning-title">Limitacion importante:</span> esta version antigua VB6 depende de Windows HTML Help, del runtime VB6 y del comportamiento clasico del portapapeles.</div>
<ul><li>El runtime de Visual Basic 6 es obligatorio.</li><li>Windows 11 fue probado para esta version de mantenimiento.</li><li>Las versiones antiguas de Windows son objetivos historicos y no siempre se vuelven a probar.</li><li>Microsoft 365 / Office 365 puede romper flujos de WizardExpress basados en portapapeles.</li></ul>
<p>Esta version esta pensada para usuarios que todavia necesitan el flujo de trabajo clasico de Syscalculator.</p>"""),
        ],
    },
    "cat": {
        "lang": "ca",
        "home": "index_cat.htm",
        "home_name": "Start",
        "title": "Syscalculator 1.74 Help - Catala",
        "intro": "Syscalculator 1.74 conserva el conversor classic de Visual Basic 6 com a versio legacy de manteniment.",
        "select": "Trieu un tema al panell de contingut o a la llista seguent.",
        "topics": [
            ("Convertir un Valor", "help_cat_convertir.htm", """<h1>Convertir un Valor</h1>
<div class="nav"><a href="index_cat.htm">Ajuda en catala</a></div>
<p>Useu la finestra principal per al treball normal de conversio. El cataleg decideix quins conversors apareixen a la llista.</p>
<ol><li>Trieu un conversor de la llista.</li><li>Escriviu el valor d'origen al primer camp.</li><li>Llegiu el resultat calculat al segon camp.</li><li>Useu la direccio inversa per calcular en sentit contrari.</li></ol>
<p>Si afegiu o repareu un conversor mentre el programa esta obert, recarregueu el cataleg o reinicieu Syscalculator.</p>
<p><a href="Syscal_3.png"><img src="Syscal_3.png" width="299" height="229" alt="Finestra principal"></a></p>"""),
            ("Camps, Edicio i Porta-retalls", "help_cat_camps.htm", """<h1>Camps, Edicio i Porta-retalls</h1>
<div class="nav"><a href="index_cat.htm">Ajuda en catala</a></div>
<p>Les ordres d'edicio treballen sobre el camp actiu. Seleccioneu text primer si nomes voleu canviar part d'un valor.</p>
<ul><li><b>Copiar</b> copia el text seleccionat.</li><li><b>Retallar</b> copia i elimina el text seleccionat.</li><li><b>Enganxar</b> insereix text del porta-retalls al camp actiu.</li><li><b>Eliminar</b> esborra el text seleccionat.</li><li><b>Seleccionar-ho tot</b> selecciona tot el valor del camp actiu.</li></ul>
<p>Els camps buits s'ignoren en les ordres especials de copiar entrada/sortida.</p>"""),
            ("WizardExpress", "help_cat_wizardexpress.htm", """<h1>WizardExpress</h1>
<div class="nav"><a href="index_cat.htm">Ajuda en catala</a></div>
<p>WizardExpress converteix valors copiats al porta-retalls. Esta pensat per a versions antigues d'Office i Word, fulls de calcul classics i text simple.</p>
<ol><li>Copieu els valors des de l'aplicacio d'origen.</li><li>Obriu WizardExpress.</li><li>Executeu la conversio.</li><li>Enganxeu els valors convertits de nou a l'aplicacio.</li></ol>
<p><b>Nota de compatibilitat:</b> LibreOffice s'ha provat i funciona correctament. Microsoft 365 / Office 365 no funciona de manera fiable amb aquesta integracio VB6 antiga del porta-retalls.</p>
<p><a href="syscal_1.png"><img src="syscal_1.png" width="969" height="593" alt="WizardExpress valors d'origen"></a></p>
<p><img src="syscal_2.png" width="947" height="631" alt="WizardExpress valors convertits"></p>"""),
            ("Calculadora", "help_cat_calculadora.htm", """<h1>Calculadora</h1>
<div class="nav"><a href="index_cat.htm">Ajuda en catala</a></div>
<p>La calculadora es una petita ajuda classica per a calculs rapids al costat de la finestra del conversor.</p>
<ul><li>Useu-la per a calculs temporals abans d'introduir un valor.</li><li>Copieu el numero final al camp del conversor quan calgui.</li><li>Mantingueu simple el format numeric quan moveu valors entre calculadora i conversor.</li></ul>
<p><a href="Syscal_3.png"><img src="Syscal_3.png" width="299" height="229" alt="Calculadora i conversor"></a></p>"""),
            ("Configuracio", "help_cat_configuracio.htm", """<h1>Configuracio</h1>
<div class="nav"><a href="index_cat.htm">Ajuda en catala</a></div>
<p>La finestra d'opcions controla idioma, conversor predeterminat i cataleg.</p>
<ul><li>Trieu el fitxer d'idioma que usa Syscalculator.</li><li>Definiu el conversor predeterminat per a l'inici.</li><li>Afegiu, elimineu o reordeneu entrades del cataleg.</li><li>Apliqueu els canvis abans de tancar la finestra d'opcions.</li></ul>
<p>En Windows modern, la configuracio es copia a la carpeta de dades de l'usuari quan es possible.</p>
<p><img src="Syscal_4.png" width="508" height="356" alt="Configuracio"></p>"""),
            ("Finestra, Safata i Decimals", "help_cat_finestra_opcions.htm", """<h1>Finestra, Safata i Decimals</h1>
<div class="nav"><a href="index_cat.htm">Ajuda en catala</a></div>
<p>Aquestes opcions controlen el comportament diari de Syscalculator.</p>
<ul><li><b>Sempre visible</b> mante la finestra principal sobre altres finestres.</li><li><b>Safata</b> permet que Syscalculator continui actiu a l'area de notificacio.</li><li><b>Iniciar amb Windows</b> inicia Syscalculator ocult a la safata.</li><li><b>Decimals</b> controla quants decimals es mostren.</li><li><b>Grup de digits</b> mostra numeros agrupats quan el conversor ho admet.</li></ul>"""),
            ("Fitxers NOD i Cataleg", "help_cat_nod_cataleg.htm", """<h1>Fitxers NOD i Cataleg</h1>
<div class="nav"><a href="index_cat.htm">Ajuda en catala</a></div>
<p>Els conversors de Syscalculator es desen com a fitxers NOD classics. El cataleg els dona nom dins de la llista principal.</p>
<ul><li>Mantingueu els fitxers NOD a les carpetes de conversors esperades.</li><li>Afegiu entrades al cataleg quan un conversor nou hagi d'apareixer a la llista.</li><li>Reinicieu Syscalculator o recarregueu el cataleg despres de canviar fitxers de conversor.</li></ul>
<p>La versio 1.74 inclou conversors classics de distancia, massa, pressio, temperatura, volum, text i monedes historiques de l'euro.</p>
<p><img src="triconvert_3.jpg" width="761" height="505" alt="Exemple de tri-conversio"></p>"""),
            ("Limitacions i Suport", "help_cat_suport.htm", """<h1>Limitacions i Suport</h1>
<div class="nav"><a href="index_cat.htm">Ajuda en catala</a></div>
<p>Syscalculator 1.74 es una versio de manteniment legacy en VB6. Conserva compatibilitat historica, pero no es una aplicacio moderna .NET.</p>
<div class="warning"><img src="warning_triangle.png" width="32" height="32" alt="!"><span class="warning-title">Limitacio important:</span> aquesta versio antiga VB6 depen de Windows HTML Help, del runtime VB6 i del comportament classic del porta-retalls.</div>
<ul><li>El runtime de Visual Basic 6 es obligatori.</li><li>Windows 11 s'ha provat per a aquesta versio de manteniment.</li><li>Les versions antigues de Windows son objectius historics i no sempre es tornen a provar.</li><li>Microsoft 365 / Office 365 pot trencar fluxos de WizardExpress basats en porta-retalls.</li></ul>
<p>Aquesta versio esta pensada per a usuaris que encara necessiten el flux de treball classic de Syscalculator.</p>"""),
        ],
    },
}

LANGUAGE_IDS = {
    "en": "0x409 English (United States)",
    "nl": "0x413 Dutch (Netherlands)",
    "es": "0xc0a Spanish (Spain)",
    "cat": "0x403 Catalan",
}


def item(name, local, indent="  "):
    return (
        f'{indent}<LI><OBJECT type="text/sitemap">\n'
        f'{indent}  <param name="Name" value="{escape(name)}">\n'
        f'{indent}  <param name="Local" value="{local}">\n'
        f"{indent}</OBJECT>"
    )


def hhc(lines):
    return """<!DOCTYPE HTML PUBLIC "-//IETF//DTD HTML//EN">
<HTML>
<HEAD>
<meta name="GENERATOR" content="Microsoft HTML Help Workshop">
</HEAD><BODY>
<OBJECT type="text/site properties">
  <param name="Window Styles" value="0x800025">
</OBJECT>
<UL>
%s
</UL>
</BODY></HTML>
""" % "\n".join(lines)


def hhk(lines):
    return """<!DOCTYPE HTML PUBLIC "-//IETF//DTD HTML//EN">
<HTML>
<HEAD>
<meta name="GENERATOR" content="Microsoft HTML Help Workshop">
</HEAD><BODY>
<UL>
%s
</UL>
</BODY></HTML>
""" % "\n".join(lines)


def hhp(compiled, contents, index_file, default_topic, title, files, language):
    return f"""[OPTIONS]
Compatibility=1.1 or later
Compiled file={compiled}
Contents file={contents}
Index file={index_file}
Default Window=main
Default topic={default_topic}
Display compile progress=No
Full-text search=Yes
Language={language}
Title={title}

[WINDOWS]
main="{title}","{contents}","{index_file}","{default_topic}",,,,,,0x63520,,0x10000c,[90,80,850,620],0x80000,,,,,,0

[FILES]
{chr(10).join(files + ASSETS)}
"""


def write_text(path, text, encoding=ENCODING):
    (ROOT / path).write_text(text, encoding=encoding)


def main():
    main_lines = [item("Inhoudsopgave", "index.htm")]
    main_keywords = [item("Syscalculator 1.74 Help", "index.htm")]
    all_files = ["index.htm"]

    for code, data in HELP.items():
        links = "\n".join(
            f'  <li><a href="{file}">{escape(title)}</a></li>'
            for title, file, _ in data["topics"]
        )
        home_body = f"""<h1>{data['home_name']}</h1>
<p>{data['intro']}</p>
<p>{data['select']}</p>
<ol>
{links}
</ol>
<p class="note">Copyright (c) 1996-2026 Tiedragon.</p>"""
        write_text(data["home"], html_page(data["title"], data["lang"], home_body))
        all_files.append(data["home"])

        language_lines = [item(data["home_name"], data["home"])]
        language_keywords = [item(data["home_name"], data["home"])]
        main_lines.append(item(data["home_name"], data["home"]))
        main_lines.append("  <UL>")
        main_keywords.append(item(data["home_name"], data["home"]))

        for title, file, body in data["topics"]:
            write_text(file, html_page(f"{title} - Syscalculator 1.74", data["lang"], body))
            all_files.append(file)
            language_lines.append(item(title, file))
            language_keywords.append(item(title, file))
            main_lines.append(item(title, file, "    "))
            main_keywords.append(item(title, file))

        main_lines.append("  </UL>")
        write_text(f"Syscalculator174-{code}.hhc", hhc(language_lines), "ascii")
        write_text(f"Syscalculator174-{code}.hhk", hhk(language_keywords), "ascii")
        language_files = [data["home"]] + [file for _, file, _ in data["topics"]]
        write_text(
            f"Syscalculator174-{code}.hhp",
            hhp(
                f"Syscalculator174-{code}.chm",
                f"Syscalculator174-{code}.hhc",
                f"Syscalculator174-{code}.hhk",
                data["home"],
                data["title"],
                language_files,
                LANGUAGE_IDS[code],
            ),
            "ascii",
        )

    main_body = """<h1>Syscalculator 1.74 Help</h1>
<p>Select a language from the contents pane or from the list below.</p>
<ul>
  <li><a href="index_en.htm">English Help</a></li>
  <li><a href="index_nl.htm">Nederlandse Help</a></li>
  <li><a href="index_es.htm">Ayuda en Espanol</a></li>
  <li><a href="index_cat.htm">Ajuda en Catala</a></li>
</ul>
<p class="note">Copyright (c) 1996-2026 Tiedragon.</p>"""
    write_text("index.htm", html_page("Syscalculator 1.74 Help Contents", "en-us", main_body))
    write_text("Syscalculator174.hhc", hhc(main_lines), "ascii")
    write_text("Syscalculator174.hhk", hhk(main_keywords), "ascii")
    write_text(
        "Syscalculator174.hhp",
        hhp(
            "Syscalculator174.chm",
            "Syscalculator174.hhc",
            "Syscalculator174.hhk",
            "index.htm",
            "Syscalculator 1.74 Help",
            all_files,
            "0x409 English (United States)",
        ),
        "ascii",
    )


if __name__ == "__main__":
    main()
