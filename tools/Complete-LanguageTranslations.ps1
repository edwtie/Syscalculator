param(
    [string]$LanguageDirectory = "src/syscalculator"
)

$ErrorActionPreference = "Stop"

# Encoding rule:
# Run this script with PowerShell 7+ (`pwsh`) to avoid Windows PowerShell 5.1
# encoding surprises. Language files written by this script remain UTF-8
# without BOM.
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptRoot "Assert-PowerShell7.ps1") -Purpose "language package translation work" -ScriptPath $PSCommandPath
$repoRoot = Split-Path -Parent $scriptRoot
$languageRoot = Join-Path $repoRoot $LanguageDirectory

function Add-MissingLanguageLines {
    param(
        [string]$Code,
        [string[]]$Lines
    )

    $path = Join-Path $languageRoot "$Code.lng"
    if (-not (Test-Path $path)) {
        throw "Language file not found: $path"
    }

    $existing = @{}
    foreach ($line in [System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8)) {
        $separator = $line.IndexOf("=")
        if ($separator -gt 0) {
            $existing[$line.Substring(0, $separator).Trim()] = $true
        }
    }

    $toAppend = New-Object System.Collections.Generic.List[string]
    foreach ($line in $Lines) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#")) {
            continue
        }

        $separator = $line.IndexOf("=")
        if ($separator -le 0) {
            continue
        }

        $key = $line.Substring(0, $separator).Trim()
        if (-not $existing.ContainsKey($key)) {
            $toAppend.Add($line)
        }
    }

    if ($toAppend.Count -eq 0) {
        Write-Host "$Code already complete for this translation batch."
        return
    }

    $content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).TrimEnd()
    $content += [Environment]::NewLine + [Environment]::NewLine
    $content += "# Beta 2 language-package coverage" + [Environment]::NewLine
    $content += ($toAppend -join [Environment]::NewLine) + [Environment]::NewLine
    [System.IO.File]::WriteAllText($path, $content, $Utf8NoBom)
    Write-Host "$Code appended $($toAppend.Count) keys."
}

$sharedKeys = @{
    deu = @'
about.license_text.daily=Syscalculator 2.0 Daily-Build. Interne Evaluierungslizenz.
about.license_text.beta=Syscalculator 2.0 Beta-Build. Interne Evaluierungslizenz.
about.license_text.stable=Syscalculator 2.0. Lizenzierte Software.
editor.nod_help.guide.intro=Dieses Kapitel beschreibt die Arbeit im Editorfenster. Sie schreiben NOD-Text in der Mitte, verwenden die Symbolleiste zum Oeffnen, Speichern, Testen und Validieren und lesen Befehlstipps, wenn der Cursor auf einer bekannten Zeile steht.
editor.nod_help.guide.window_title=Das Fenster
editor.nod_help.guide.screenshot_caption=Beispiel: ein Celsius/Fahrenheit-Konverter mit dem Befehlstipp fuer <code>Name</code>.
editor.nod_help.guide.create_title=Konverter erstellen
editor.nod_help.guide.create.name=Beginnen Sie mit <a class="cmd-link" href="nodpage:cmd:name"><code>Name</code></a>, <a class="cmd-link" href="nodpage:cmd:input1"><code>input1</code></a> und <a class="cmd-link" href="nodpage:cmd:input2"><code>input2</code></a>.
editor.nod_help.guide.create.symbols=Fuegen Sie optional <a class="cmd-link" href="nodpage:cmd:symb1"><code>Symb1</code></a> bis <a class="cmd-link" href="nodpage:cmd:symb4"><code>Symb4</code></a> fuer Symbole links oder rechts von Werten hinzu.
editor.nod_help.guide.create.rules=Schreiben Sie die Umrechnung mit <a class="cmd-link" href="nodpage:cmd:math"><code>math</code></a>, <a class="cmd-link" href="nodpage:cmd:trans"><code>trans</code></a> oder <a class="cmd-link" href="nodpage:cmd:chg"><code>chg</code></a>.
editor.nod_help.guide.create.validate=Verwenden Sie <b>Validate</b>, um Syntaxfehler zu finden, und <b>Test</b>, um das Ergebnis auszuprobieren.
editor.nod_help.guide.create.save=Speichern Sie die Datei, wenn der Konverter korrekt ist.
editor.nod_help.guide.tips_title=Befehlstipps
editor.nod_help.guide.tips=Die blaue Sprechblase zeigt kurze Hilfe fuer die Zeile, in der der Cursor steht. Manchmal zeigt sie auch eine modernere Form. Verwenden Sie <b>Replace</b> nur, wenn Sie diese moderne Form wirklich wollen; <b>More help</b> oeffnet die vollstaendige Befehlsseite.
editor.nod_help.guide.undo_title=Rueckgaengig, Wiederholen und Wiederherstellen
editor.nod_help.guide.undo=<b>Undo</b> und <b>Redo</b> betreffen die letzten Textaenderungen. <b>Restore</b> ist groesser: es setzt die aktuelle Registerkarte auf die zuletzt geoeffnete oder gespeicherte Version zurueck. <b>Tools &gt; Repair lines</b> ist etwas anderes; es versucht eingefuegte oder zusammengefuegte NOD-Zeilen zu reparieren.
formula_card.formula_film=Formelfilm
formula_card.formula_film_title=Formelfilm 2.1 Experiment
formula_card.formula_film_subtitle=Forschung fuer bewegte Formelkarten und Lehrfilme
legal.last_updated.label=Zuletzt aktualisiert:
legal.last_updated.date=21. Mai 2026
legal.license.title=Lizenzvereinbarung
legal.license.intro=Diese Lizenzvereinbarung gilt fuer Syscalculator 2.0, veroeffentlicht von Edward Tie / Tiedragon.
legal.license.scope.title=Geltungsbereich
legal.license.scope.body=Syscalculator ist lizenzierte Software. Daily- und Beta-Builds sind Evaluierungsversionen und koennen waehrend der Entwicklung geaendert, ersetzt oder zurueckgezogen werden.
legal.license.daily.title=Daily- und Beta-Builds
legal.license.daily.body=Daily- und Beta-Builds werden fuer Tests, Feedback und Evaluierung bereitgestellt. Sie koennen unfertige Funktionen, Regressionen oder Diagnoseverhalten enthalten.
legal.license.use.title=Erlaubte Nutzung
legal.license.use.personal=Sie duerfen Syscalculator fuer persoenliche Arbeit, Tests und Lernen installieren und verwenden.
legal.license.use.internal=Organisationen duerfen Syscalculator intern evaluieren, sofern keine separate schriftliche Vereinbarung etwas anderes festlegt.
legal.license.use.no_resale=Sie duerfen Syscalculator nicht als eigenes Produkt verkaufen, vermieten, unterlizenzieren oder weiterverteilen.
legal.license.ownership.title=Eigentum
legal.license.ownership.body=Syscalculator, der Name Tiedragon, Anwendungscode, Graphmodule, Hilfetexte und zugehoerige Assets bleiben Eigentum von Edward Tie / Tiedragon, sofern eine Datei nicht ausdruecklich eine andere Lizenz nennt.
legal.license.warranty.title=Keine Gewaehrleistung
legal.license.warranty.body=Syscalculator wird wie besehen bereitgestellt. Berechnungen, Umrechnungen und erzeugte Ausgaben sollten vom Benutzer geprueft werden, bevor sie fuer wichtige Entscheidungen verwendet werden.
legal.license.liability.title=Haftungsbeschraenkung
legal.license.liability.body=Soweit gesetzlich zulaessig haftet Tiedragon nicht fuer indirekte Schaeden, Datenverlust, Betriebsunterbrechung oder Entscheidungen auf Basis ungepruefter Ausgaben.
legal.license.third_party.title=Drittanbieterkomponenten
legal.license.third_party.body=Syscalculator kann Drittanbieterkomponenten wie .NET, WebView2, GitHub-Verteilungsdienste oder Microsoft-Plattformkomponenten verwenden. Fuer diese Komponenten gelten ihre eigenen Bedingungen.
legal.license.contact.title=Kontakt
legal.license.contact.body=Fragen zur Lizenz koennen an info@tiedragon.com gesendet werden.
legal.privacy.title=Datenschutzerklaerung
legal.privacy.intro=Diese Datenschutzerklaerung gilt fuer Syscalculator, veroeffentlicht von Edward Tie / Tiedragon.
legal.privacy.summary.title=Kurze Zusammenfassung
legal.privacy.summary.body=Syscalculator ist eine Desktopanwendung. Berechnungen, Umrechnungen, NOD-Dateien und Einstellungen werden normalerweise lokal auf Ihrem Computer verarbeitet. Syscalculator verkauft keine personenbezogenen Daten und verwendet keine Werbung oder Analyseverfolgung.
legal.privacy.local.title=Lokale Daten
legal.privacy.local.body=Syscalculator kann lokale Daten speichern, die fuer die Nutzung der Anwendung erforderlich sind.
legal.privacy.local.settings=Anwendungseinstellungen.
legal.privacy.local.language=Ausgewaehlte Sprache und Updatekanal.
legal.privacy.local.recent=Zuletzt verwendete Dateien und Updatestatus.
legal.privacy.local.files=NOD-Dateien, die Sie erstellen oder oeffnen.
legal.privacy.feedback.title=Feedback
legal.privacy.feedback.body=Wenn Sie das Feedbackformular verwenden, werden die eingegebenen Informationen an Tiedragon gesendet.
legal.privacy.feedback.type=Feedbacktyp.
legal.privacy.feedback.name=Name, falls Sie einen eingeben.
legal.privacy.feedback.email=E-Mail-Adresse, falls Sie eine eingeben.
legal.privacy.feedback.message=Betreff und Nachrichtentext.
legal.privacy.feedback.support=Optionale Supportinformationen wie Produktversion, Releasekanal, Betriebssystem, .NET-Version und Prozessorarchitektur.
legal.privacy.updates.title=Updates
legal.privacy.updates.body=Wenn Syscalculator nach Updates sucht, kann es ein Update-Manifest von tiedragon.com herunterladen. Der Server kann technisch notwendige Daten wie IP-Adresse, Zeitpunkt der Anfrage und normale Serverprotokolldaten erhalten.
legal.privacy.use.title=Wie Daten verwendet werden
legal.privacy.use.body=Daten werden verwendet, damit Syscalculator funktioniert, lokale Einstellungen speichert, Feedback- oder Supportanfragen beantwortet, die Software verbessert und Updates bereitstellt.
legal.privacy.sharing.title=Weitergabe an Dritte
legal.privacy.sharing.body=Tiedragon verkauft keine personenbezogenen Daten. Daten duerfen nur von technischen Dienstleistern verarbeitet werden, die fuer Hosting, E-Mail, Downloads oder Softwareverteilung erforderlich sind.
legal.privacy.retention.title=Aufbewahrung
legal.privacy.retention.body=Lokale Daten bleiben erhalten, bis Sie sie loeschen oder Syscalculator entfernen. Feedbacknachrichten werden nicht laenger aufbewahrt als fuer Support, Fehlerbehebung und Verwaltung notwendig.
legal.privacy.choices.title=Ihre Wahlmoeglichkeiten
legal.privacy.choices.no_name=Sie koennen Feedback senden, ohne einen Namen einzugeben.
legal.privacy.choices.support=Sie koennen Supportinformationen im Feedbackformular ausschalten.
legal.privacy.choices.updates=Sie koennen automatische Updatepruefungen deaktivieren oder den Updatekanal aendern.
legal.privacy.choices.local=Sie koennen lokale Dateien und Einstellungen selbst entfernen.
legal.privacy.contact.title=Kontakt
legal.privacy.contact.body=Bei Fragen zu Datenschutz oder Datenverarbeitung kontaktieren Sie info@tiedragon.com.
menu.about.help=Hilfe fuer Benutzer
wizard.menu.file=Datei
wizard.menu.file.close=Schliessen
wizard.menu.extra=Extra
wizard.menu.extra.advanced=Erweiterter Modus
wizard.menu.extra.data=Daten im Excel-Stil
wizard.menu.extra.debug=Zwischenablage-Debug
wizard.advanced.title=WizardExpress-Daten
wizard.advanced.copy_tsv=Ausgabe-TSV kopieren
wizard.mode.basic=Basismodus
wizard.subtitle=Zwischenablage hinein, Umrechnung heraus, mit dem alten Ampelgefuehl.
wizard.copy_now=Jetzt kopieren
wizard.input=Eingabe
wizard.output=Ausgabe
wizard.status.caption=Status
'@ -split "`n"
    fra = @'
about.license_text.daily=Syscalculator 2.0 version daily. Licence d'evaluation interne.
about.license_text.beta=Syscalculator 2.0 version beta. Licence d'evaluation interne.
about.license_text.stable=Syscalculator 2.0. Logiciel sous licence.
editor.nod_help.guide.intro=Ce chapitre explique le travail dans la fenetre de l'editeur. Vous ecrivez le texte NOD au centre, utilisez la barre d'outils pour ouvrir, enregistrer, tester et valider, et lisez les conseils de commande lorsque le curseur se trouve sur une ligne connue.
editor.nod_help.guide.window_title=La fenetre
editor.nod_help.guide.screenshot_caption=Exemple : un convertisseur Celsius/Fahrenheit avec le conseil de commande pour <code>Name</code>.
editor.nod_help.guide.create_title=Creer un convertisseur
editor.nod_help.guide.create.name=Commencez par <a class="cmd-link" href="nodpage:cmd:name"><code>Name</code></a>, <a class="cmd-link" href="nodpage:cmd:input1"><code>input1</code></a> et <a class="cmd-link" href="nodpage:cmd:input2"><code>input2</code></a>.
editor.nod_help.guide.create.symbols=Ajoutez au besoin <a class="cmd-link" href="nodpage:cmd:symb1"><code>Symb1</code></a> a <a class="cmd-link" href="nodpage:cmd:symb4"><code>Symb4</code></a> pour les symboles a gauche ou a droite des valeurs.
editor.nod_help.guide.create.rules=Ecrivez la conversion avec <a class="cmd-link" href="nodpage:cmd:math"><code>math</code></a>, <a class="cmd-link" href="nodpage:cmd:trans"><code>trans</code></a> ou <a class="cmd-link" href="nodpage:cmd:chg"><code>chg</code></a>.
editor.nod_help.guide.create.validate=Utilisez <b>Validate</b> pour trouver les erreurs de syntaxe et <b>Test</b> pour essayer le resultat.
editor.nod_help.guide.create.save=Enregistrez le fichier lorsque le convertisseur est correct.
editor.nod_help.guide.tips_title=Conseils de commande
editor.nod_help.guide.tips=La bulle bleue affiche une aide courte pour la ligne ou se trouve le curseur. Elle montre parfois aussi une forme plus moderne. Utilisez <b>Replace</b> seulement si vous voulez vraiment cette forme moderne ; <b>More help</b> ouvre la page complete de la commande.
editor.nod_help.guide.undo_title=Annuler, retablir et restaurer
editor.nod_help.guide.undo=<b>Undo</b> et <b>Redo</b> concernent les modifications recentes du texte. <b>Restore</b> est plus large : il remet l'onglet courant a la derniere version ouverte ou enregistree. <b>Tools &gt; Repair lines</b> est different ; il essaie de reparer des lignes NOD collees ou fusionnees.
formula_card.formula_film=Film de formule
formula_card.formula_film_title=Experience Formula film 2.1
formula_card.formula_film_subtitle=Recherche pour cartes de formules animees et films pedagogiques
legal.last_updated.label=Derniere mise a jour :
legal.last_updated.date=21 mai 2026
legal.license.title=Contrat de licence
legal.license.intro=Ce contrat de licence s'applique a Syscalculator 2.0, publie par Edward Tie / Tiedragon.
legal.license.scope.title=Portee
legal.license.scope.body=Syscalculator est un logiciel sous licence. Les versions daily et beta sont des versions d'evaluation et peuvent etre modifiees, remplacees ou retirees pendant le developpement.
legal.license.daily.title=Versions daily et beta
legal.license.daily.body=Les versions daily et beta sont fournies pour les tests, les retours et l'evaluation. Elles peuvent contenir des fonctions inachevees, des regressions ou un comportement de diagnostic.
legal.license.use.title=Utilisation autorisee
legal.license.use.personal=Vous pouvez installer et utiliser Syscalculator pour un travail personnel, des tests et l'apprentissage.
legal.license.use.internal=Les organisations peuvent evaluer Syscalculator en interne sauf accord ecrit separe contraire.
legal.license.use.no_resale=Vous ne pouvez pas vendre, louer, sous-licencier ou redistribuer Syscalculator comme votre propre produit.
legal.license.ownership.title=Propriete
legal.license.ownership.body=Syscalculator, le nom Tiedragon, le code de l'application, les modules graphiques, le contenu d'aide et les ressources associees restent la propriete d'Edward Tie / Tiedragon, sauf si un fichier indique explicitement une autre licence.
legal.license.warranty.title=Aucune garantie
legal.license.warranty.body=Syscalculator est fourni tel quel. Les calculs, conversions et sorties generees doivent etre verifies par l'utilisateur avant d'etre utilises pour des decisions importantes.
legal.license.liability.title=Limitation de responsabilite
legal.license.liability.body=Dans toute la mesure permise par la loi, Tiedragon n'est pas responsable des dommages indirects, pertes de donnees, interruptions d'activite ou decisions prises a partir de sorties non verifiees.
legal.license.third_party.title=Composants tiers
legal.license.third_party.body=Syscalculator peut utiliser des composants tiers tels que .NET, WebView2, les services de distribution GitHub ou des composants de plateforme Microsoft. Ces composants restent soumis a leurs propres conditions.
legal.license.contact.title=Contact
legal.license.contact.body=Les questions sur la licence peuvent etre envoyees a info@tiedragon.com.
legal.privacy.title=Declaration de confidentialite
legal.privacy.intro=Cette declaration de confidentialite s'applique a Syscalculator, publie par Edward Tie / Tiedragon.
legal.privacy.summary.title=Resume court
legal.privacy.summary.body=Syscalculator est une application de bureau. Les calculs, conversions, fichiers NOD et parametres sont normalement traites localement sur votre ordinateur. Syscalculator ne vend pas de donnees personnelles et n'utilise pas de publicite ni de suivi analytique.
legal.privacy.local.title=Donnees locales
legal.privacy.local.body=Syscalculator peut stocker des donnees locales necessaires a l'utilisation de l'application.
legal.privacy.local.settings=Parametres de l'application.
legal.privacy.local.language=Langue selectionnee et canal de mise a jour.
legal.privacy.local.recent=Fichiers recents et etat des mises a jour.
legal.privacy.local.files=Fichiers NOD que vous creez ou ouvrez.
legal.privacy.feedback.title=Retour
legal.privacy.feedback.body=Si vous utilisez le formulaire de retour, les informations saisies sont envoyees a Tiedragon.
legal.privacy.feedback.type=Type de retour.
legal.privacy.feedback.name=Nom, si vous en saisissez un.
legal.privacy.feedback.email=Adresse e-mail, si vous en saisissez une.
legal.privacy.feedback.message=Sujet et texte du message.
legal.privacy.feedback.support=Informations de support facultatives comme la version du produit, le canal de publication, le systeme d'exploitation, la version .NET et l'architecture du processeur.
legal.privacy.updates.title=Mises a jour
legal.privacy.updates.body=Lorsque Syscalculator recherche des mises a jour, il peut telecharger un manifeste depuis tiedragon.com. Le serveur peut recevoir des donnees techniquement necessaires comme l'adresse IP, l'heure de la demande et les journaux serveur standard.
legal.privacy.use.title=Utilisation des donnees
legal.privacy.use.body=Les donnees servent a faire fonctionner Syscalculator, enregistrer les parametres locaux, repondre aux demandes de retour ou de support, ameliorer le logiciel et fournir des mises a jour.
legal.privacy.sharing.title=Partage avec des tiers
legal.privacy.sharing.body=Tiedragon ne vend pas de donnees personnelles. Les donnees peuvent seulement etre traitees par des prestataires techniques necessaires pour l'hebergement, l'e-mail, les telechargements ou la distribution logicielle.
legal.privacy.retention.title=Conservation
legal.privacy.retention.body=Les donnees locales restent jusqu'a ce que vous les supprimiez ou retiriez Syscalculator. Les messages de retour sont conserves seulement le temps necessaire au support, au depannage et a l'administration.
legal.privacy.choices.title=Vos choix
legal.privacy.choices.no_name=Vous pouvez envoyer un retour sans saisir de nom.
legal.privacy.choices.support=Vous pouvez desactiver les informations de support dans le formulaire de retour.
legal.privacy.choices.updates=Vous pouvez desactiver les recherches automatiques de mises a jour ou changer le canal de mise a jour.
legal.privacy.choices.local=Vous pouvez supprimer vous-meme les fichiers et parametres locaux.
legal.privacy.contact.title=Contact
legal.privacy.contact.body=Pour les questions de confidentialite ou de traitement des donnees, contactez info@tiedragon.com.
menu.about.help=Aide pour les utilisateurs
wizard.menu.file=Fichier
wizard.menu.file.close=Fermer
wizard.menu.extra=Extra
wizard.menu.extra.advanced=Mode avance
wizard.menu.extra.data=Donnees style Excel
wizard.menu.extra.debug=Debogage du presse-papiers
wizard.advanced.title=Donnees WizardExpress
wizard.advanced.copy_tsv=Copier la sortie TSV
wizard.mode.basic=Mode de base
wizard.subtitle=Presse-papiers en entree, conversion en sortie, avec l'ancien style feu tricolore.
wizard.copy_now=Copier maintenant
wizard.input=Entree
wizard.output=Sortie
wizard.status.caption=Etat
'@ -split "`n"
    ind = @'
about.license_text.daily=Syscalculator 2.0 build daily. Lisensi evaluasi internal.
about.license_text.beta=Syscalculator 2.0 build beta. Lisensi evaluasi internal.
about.license_text.stable=Syscalculator 2.0. Perangkat lunak berlisensi.
editor.nod_help.guide.intro=Bab ini menjelaskan cara bekerja di jendela editor. Anda menulis teks NOD di tengah, memakai toolbar untuk membuka, menyimpan, menguji dan memvalidasi, lalu membaca tips perintah saat kursor berada pada baris yang dikenal.
editor.nod_help.guide.window_title=Jendela
editor.nod_help.guide.screenshot_caption=Contoh: konverter Celsius/Fahrenheit dengan tips perintah untuk <code>Name</code>.
editor.nod_help.guide.create_title=Membuat konverter
editor.nod_help.guide.create.name=Mulai dengan <a class="cmd-link" href="nodpage:cmd:name"><code>Name</code></a>, <a class="cmd-link" href="nodpage:cmd:input1"><code>input1</code></a> dan <a class="cmd-link" href="nodpage:cmd:input2"><code>input2</code></a>.
editor.nod_help.guide.create.symbols=Tambahkan opsional <a class="cmd-link" href="nodpage:cmd:symb1"><code>Symb1</code></a> sampai <a class="cmd-link" href="nodpage:cmd:symb4"><code>Symb4</code></a> untuk simbol di kiri atau kanan nilai.
editor.nod_help.guide.create.rules=Tulis konversi dengan <a class="cmd-link" href="nodpage:cmd:math"><code>math</code></a>, <a class="cmd-link" href="nodpage:cmd:trans"><code>trans</code></a> atau <a class="cmd-link" href="nodpage:cmd:chg"><code>chg</code></a>.
editor.nod_help.guide.create.validate=Gunakan <b>Validate</b> untuk menemukan kesalahan sintaks dan <b>Test</b> untuk mencoba hasilnya.
editor.nod_help.guide.create.save=Simpan file ketika konverter sudah benar.
editor.nod_help.guide.tips_title=Tips perintah
editor.nod_help.guide.tips=Balon biru memberi bantuan singkat untuk baris tempat kursor berada. Kadang juga menampilkan bentuk yang lebih modern. Gunakan <b>Replace</b> hanya jika Anda benar-benar menginginkan bentuk modern itu; <b>More help</b> membuka halaman perintah lengkap.
editor.nod_help.guide.undo_title=Urungkan, ulangi dan pulihkan
editor.nod_help.guide.undo=<b>Undo</b> dan <b>Redo</b> berlaku untuk perubahan teks terbaru. <b>Restore</b> lebih besar: mengembalikan tab saat ini ke versi terakhir yang dibuka atau disimpan. <b>Tools &gt; Repair lines</b> berbeda; fitur itu mencoba memperbaiki baris NOD yang ditempel atau digabung.
formula_card.formula_film=Film formula
formula_card.formula_film_title=Eksperimen film formula 2.1
formula_card.formula_film_subtitle=Riset untuk kartu formula bergerak dan film pembelajaran
legal.last_updated.label=Terakhir diperbarui:
legal.last_updated.date=21 Mei 2026
legal.license.title=Perjanjian Lisensi
legal.license.intro=Perjanjian lisensi ini berlaku untuk Syscalculator 2.0, diterbitkan oleh Edward Tie / Tiedragon.
legal.license.scope.title=Ruang lingkup
legal.license.scope.body=Syscalculator adalah perangkat lunak berlisensi. Build daily dan beta adalah build evaluasi dan dapat diubah, diganti atau ditarik selama pengembangan.
legal.license.daily.title=Build Daily dan Beta
legal.license.daily.body=Build daily dan beta disediakan untuk pengujian, masukan dan evaluasi. Build ini dapat berisi fitur yang belum selesai, regresi atau perilaku diagnostik.
legal.license.use.title=Penggunaan yang diizinkan
legal.license.use.personal=Anda dapat memasang dan memakai Syscalculator untuk pekerjaan pribadi, pengujian dan belajar.
legal.license.use.internal=Organisasi dapat mengevaluasi Syscalculator secara internal kecuali ada perjanjian tertulis terpisah yang menyatakan lain.
legal.license.use.no_resale=Anda tidak boleh menjual, menyewakan, mensublisensikan atau mendistribusikan ulang Syscalculator sebagai produk Anda sendiri.
legal.license.ownership.title=Kepemilikan
legal.license.ownership.body=Syscalculator, nama Tiedragon, kode aplikasi, modul graph, konten bantuan dan aset terkait tetap menjadi milik Edward Tie / Tiedragon kecuali sebuah file secara eksplisit menyatakan lisensi lain.
legal.license.warranty.title=Tanpa garansi
legal.license.warranty.body=Syscalculator disediakan apa adanya. Perhitungan, konversi dan output yang dibuat harus diperiksa pengguna sebelum dipakai untuk keputusan penting.
legal.license.liability.title=Pembatasan tanggung jawab
legal.license.liability.body=Sejauh diizinkan hukum, Tiedragon tidak bertanggung jawab atas kerusakan tidak langsung, kehilangan data, gangguan bisnis atau keputusan yang dibuat dari output yang belum diperiksa.
legal.license.third_party.title=Komponen pihak ketiga
legal.license.third_party.body=Syscalculator dapat memakai komponen pihak ketiga seperti .NET, WebView2, layanan distribusi GitHub atau komponen platform Microsoft. Komponen itu tetap tunduk pada syarat masing-masing.
legal.license.contact.title=Kontak
legal.license.contact.body=Pertanyaan tentang lisensi dapat dikirim ke info@tiedragon.com.
legal.privacy.title=Pernyataan Privasi
legal.privacy.intro=Pernyataan privasi ini berlaku untuk Syscalculator, diterbitkan oleh Edward Tie / Tiedragon.
legal.privacy.summary.title=Ringkasan singkat
legal.privacy.summary.body=Syscalculator adalah aplikasi desktop. Perhitungan, konversi, file NOD dan pengaturan biasanya diproses secara lokal di komputer Anda. Syscalculator tidak menjual data pribadi dan tidak memakai iklan atau pelacakan analitik.
legal.privacy.local.title=Data lokal
legal.privacy.local.body=Syscalculator dapat menyimpan data lokal yang diperlukan untuk memakai aplikasi.
legal.privacy.local.settings=Pengaturan aplikasi.
legal.privacy.local.language=Bahasa dan kanal update yang dipilih.
legal.privacy.local.recent=File terbaru dan status update.
legal.privacy.local.files=File NOD yang Anda buat atau buka.
legal.privacy.feedback.title=Masukan
legal.privacy.feedback.body=Jika Anda memakai formulir masukan, informasi yang Anda isi dikirim ke Tiedragon.
legal.privacy.feedback.type=Jenis masukan.
legal.privacy.feedback.name=Nama, jika Anda mengisinya.
legal.privacy.feedback.email=Alamat e-mail, jika Anda mengisinya.
legal.privacy.feedback.message=Subjek dan teks pesan.
legal.privacy.feedback.support=Informasi dukungan opsional seperti versi produk, kanal rilis, sistem operasi, versi .NET dan arsitektur prosesor.
legal.privacy.updates.title=Update
legal.privacy.updates.body=Saat Syscalculator memeriksa update, aplikasi dapat mengunduh manifest update dari tiedragon.com. Server dapat menerima data teknis yang diperlukan seperti alamat IP, waktu permintaan dan data log server standar.
legal.privacy.use.title=Cara data digunakan
legal.privacy.use.body=Data digunakan agar Syscalculator bekerja, menyimpan pengaturan lokal, menjawab masukan atau permintaan dukungan, memperbaiki perangkat lunak dan menyediakan update.
legal.privacy.sharing.title=Berbagi dengan pihak ketiga
legal.privacy.sharing.body=Tiedragon tidak menjual data pribadi. Data hanya dapat diproses oleh penyedia layanan teknis yang diperlukan untuk hosting, e-mail, unduhan atau distribusi perangkat lunak.
legal.privacy.retention.title=Penyimpanan
legal.privacy.retention.body=Data lokal tetap ada sampai Anda menghapusnya atau menghapus Syscalculator. Pesan masukan disimpan tidak lebih lama dari yang diperlukan untuk dukungan, pemecahan masalah dan administrasi.
legal.privacy.choices.title=Pilihan Anda
legal.privacy.choices.no_name=Anda dapat mengirim masukan tanpa mengisi nama.
legal.privacy.choices.support=Anda dapat mematikan informasi dukungan di formulir masukan.
legal.privacy.choices.updates=Anda dapat menonaktifkan pemeriksaan update otomatis atau mengubah kanal update.
legal.privacy.choices.local=Anda dapat menghapus file dan pengaturan lokal sendiri.
legal.privacy.contact.title=Kontak
legal.privacy.contact.body=Untuk pertanyaan privasi atau pemrosesan data, hubungi info@tiedragon.com.
menu.about.help=Bantuan untuk pengguna
wizard.menu.file=File
wizard.menu.file.close=Tutup
wizard.menu.extra=Ekstra
wizard.menu.extra.advanced=Mode lanjutan
wizard.menu.extra.data=Data gaya Excel
wizard.menu.extra.debug=Debug clipboard
wizard.advanced.title=Data WizardExpress
wizard.advanced.copy_tsv=Salin output TSV
wizard.mode.basic=Mode dasar
wizard.subtitle=Clipboard masuk, konversi keluar, dengan rasa lampu lalu lintas lama.
wizard.copy_now=Salin sekarang
wizard.input=Input
wizard.output=Output
wizard.status.caption=Status
'@ -split "`n"
    ita = @'
about.license_text.daily=Syscalculator 2.0 build daily. Licenza di valutazione interna.
about.license_text.beta=Syscalculator 2.0 build beta. Licenza di valutazione interna.
about.license_text.stable=Syscalculator 2.0. Software con licenza.
editor.nod_help.guide.intro=Questo capitolo descrive il lavoro nella finestra dell'editor. Scrivi il testo NOD al centro, usi la barra degli strumenti per aprire, salvare, testare e validare, e leggi i suggerimenti dei comandi quando il cursore si trova su una riga conosciuta.
editor.nod_help.guide.window_title=La finestra
editor.nod_help.guide.screenshot_caption=Esempio: un convertitore Celsius/Fahrenheit con il suggerimento del comando per <code>Name</code>.
editor.nod_help.guide.create_title=Creare un convertitore
editor.nod_help.guide.create.name=Inizia con <a class="cmd-link" href="nodpage:cmd:name"><code>Name</code></a>, <a class="cmd-link" href="nodpage:cmd:input1"><code>input1</code></a> e <a class="cmd-link" href="nodpage:cmd:input2"><code>input2</code></a>.
editor.nod_help.guide.create.symbols=Aggiungi opzionalmente <a class="cmd-link" href="nodpage:cmd:symb1"><code>Symb1</code></a> fino a <a class="cmd-link" href="nodpage:cmd:symb4"><code>Symb4</code></a> per simboli a sinistra o a destra dei valori.
editor.nod_help.guide.create.rules=Scrivi la conversione con <a class="cmd-link" href="nodpage:cmd:math"><code>math</code></a>, <a class="cmd-link" href="nodpage:cmd:trans"><code>trans</code></a> o <a class="cmd-link" href="nodpage:cmd:chg"><code>chg</code></a>.
editor.nod_help.guide.create.validate=Usa <b>Validate</b> per trovare errori di sintassi e <b>Test</b> per provare il risultato.
editor.nod_help.guide.create.save=Salva il file quando il convertitore e corretto.
editor.nod_help.guide.tips_title=Suggerimenti dei comandi
editor.nod_help.guide.tips=Il fumetto blu mostra un breve aiuto per la riga in cui si trova il cursore. A volte mostra anche una forma piu moderna. Usa <b>Replace</b> solo quando vuoi davvero quella forma moderna; <b>More help</b> apre la pagina completa del comando.
editor.nod_help.guide.undo_title=Annulla, ripeti e ripristina
editor.nod_help.guide.undo=<b>Undo</b> e <b>Redo</b> riguardano le modifiche recenti al testo. <b>Restore</b> e piu ampio: riporta la scheda corrente all'ultima versione aperta o salvata. <b>Tools &gt; Repair lines</b> e diverso; prova a riparare righe NOD incollate o unite.
formula_card.formula_film=Film formula
formula_card.formula_film_title=Esperimento Formula film 2.1
formula_card.formula_film_subtitle=Ricerca per schede formula animate e film didattici
legal.last_updated.label=Ultimo aggiornamento:
legal.last_updated.date=21 maggio 2026
legal.license.title=Contratto di licenza
legal.license.intro=Questo contratto di licenza si applica a Syscalculator 2.0, pubblicato da Edward Tie / Tiedragon.
legal.license.scope.title=Ambito
legal.license.scope.body=Syscalculator e software con licenza. Le build daily e beta sono build di valutazione e possono essere modificate, sostituite o ritirate durante lo sviluppo.
legal.license.daily.title=Build daily e beta
legal.license.daily.body=Le build daily e beta sono fornite per test, feedback e valutazione. Possono contenere funzioni non finite, regressioni o comportamento diagnostico.
legal.license.use.title=Uso consentito
legal.license.use.personal=Puoi installare e usare Syscalculator per lavoro personale, test e apprendimento.
legal.license.use.internal=Le organizzazioni possono valutare Syscalculator internamente salvo diverso accordo scritto separato.
legal.license.use.no_resale=Non puoi vendere, noleggiare, concedere in sublicenza o ridistribuire Syscalculator come tuo prodotto.
legal.license.ownership.title=Proprieta
legal.license.ownership.body=Syscalculator, il nome Tiedragon, il codice dell'applicazione, i moduli grafici, il contenuto della guida e le risorse correlate restano proprieta di Edward Tie / Tiedragon salvo che un file dichiari esplicitamente un'altra licenza.
legal.license.warranty.title=Nessuna garanzia
legal.license.warranty.body=Syscalculator viene fornito cosi com'e. Calcoli, conversioni e output generati devono essere controllati dall'utente prima di essere usati per decisioni importanti.
legal.license.liability.title=Limitazione di responsabilita
legal.license.liability.body=Nella misura massima consentita dalla legge, Tiedragon non e responsabile per danni indiretti, perdita di dati, interruzione dell'attivita o decisioni prese da output non controllato.
legal.license.third_party.title=Componenti di terze parti
legal.license.third_party.body=Syscalculator puo usare componenti di terze parti come .NET, WebView2, servizi di distribuzione GitHub o componenti della piattaforma Microsoft. Tali componenti restano soggetti ai propri termini.
legal.license.contact.title=Contatto
legal.license.contact.body=Le domande sulla licenza possono essere inviate a info@tiedragon.com.
legal.privacy.title=Informativa sulla privacy
legal.privacy.intro=Questa informativa sulla privacy si applica a Syscalculator, pubblicato da Edward Tie / Tiedragon.
legal.privacy.summary.title=Breve riepilogo
legal.privacy.summary.body=Syscalculator e un'applicazione desktop. Calcoli, conversioni, file NOD e impostazioni sono normalmente elaborati localmente sul computer. Syscalculator non vende dati personali e non usa pubblicita o tracciamento analitico.
legal.privacy.local.title=Dati locali
legal.privacy.local.body=Syscalculator puo memorizzare dati locali necessari per usare l'applicazione.
legal.privacy.local.settings=Impostazioni dell'applicazione.
legal.privacy.local.language=Lingua selezionata e canale di aggiornamento.
legal.privacy.local.recent=File recenti e stato degli aggiornamenti.
legal.privacy.local.files=File NOD che crei o apri.
legal.privacy.feedback.title=Feedback
legal.privacy.feedback.body=Se usi il modulo di feedback, le informazioni inserite vengono inviate a Tiedragon.
legal.privacy.feedback.type=Tipo di feedback.
legal.privacy.feedback.name=Nome, se lo inserisci.
legal.privacy.feedback.email=Indirizzo e-mail, se lo inserisci.
legal.privacy.feedback.message=Oggetto e testo del messaggio.
legal.privacy.feedback.support=Informazioni di supporto opzionali come versione del prodotto, canale di rilascio, sistema operativo, versione .NET e architettura del processore.
legal.privacy.updates.title=Aggiornamenti
legal.privacy.updates.body=Quando Syscalculator controlla gli aggiornamenti, puo scaricare un manifesto da tiedragon.com. Il server puo ricevere dati tecnicamente necessari come indirizzo IP, ora della richiesta e normali dati di log del server.
legal.privacy.use.title=Come vengono usati i dati
legal.privacy.use.body=I dati vengono usati per far funzionare Syscalculator, salvare impostazioni locali, rispondere a richieste di feedback o supporto, migliorare il software e fornire aggiornamenti.
legal.privacy.sharing.title=Condivisione con terze parti
legal.privacy.sharing.body=Tiedragon non vende dati personali. I dati possono essere trattati solo da fornitori tecnici necessari per hosting, e-mail, download o distribuzione software.
legal.privacy.retention.title=Conservazione
legal.privacy.retention.body=I dati locali restano finche non li elimini o rimuovi Syscalculator. I messaggi di feedback sono conservati non oltre quanto necessario per supporto, risoluzione problemi e amministrazione.
legal.privacy.choices.title=Le tue scelte
legal.privacy.choices.no_name=Puoi inviare feedback senza inserire un nome.
legal.privacy.choices.support=Puoi disattivare le informazioni di supporto nel modulo di feedback.
legal.privacy.choices.updates=Puoi disattivare il controllo automatico degli aggiornamenti o cambiare canale.
legal.privacy.choices.local=Puoi rimuovere autonomamente file e impostazioni locali.
legal.privacy.contact.title=Contatto
legal.privacy.contact.body=Per domande sulla privacy o sul trattamento dei dati, contatta info@tiedragon.com.
menu.about.help=Aiuto per gli utenti
wizard.menu.file=File
wizard.menu.file.close=Chiudi
wizard.menu.extra=Extra
wizard.menu.extra.advanced=Modalita avanzata
wizard.menu.extra.data=Dati stile Excel
wizard.menu.extra.debug=Debug appunti
wizard.advanced.title=Dati WizardExpress
wizard.advanced.copy_tsv=Copia output TSV
wizard.mode.basic=Modalita base
wizard.subtitle=Appunti in ingresso, conversione in uscita, con il vecchio stile semaforo.
wizard.copy_now=Copia ora
wizard.input=Input
wizard.output=Output
wizard.status.caption=Stato
'@ -split "`n"
    por = @'
about.license_text.daily=Syscalculator 2.0 build daily. Licenca de avaliacao interna.
about.license_text.beta=Syscalculator 2.0 build beta. Licenca de avaliacao interna.
about.license_text.stable=Syscalculator 2.0. Software licenciado.
editor.nod_help.guide.intro=Este capitulo explica o trabalho na janela do editor. Voce escreve texto NOD no centro, usa a barra de ferramentas para abrir, salvar, testar e validar, e le dicas de comandos quando o cursor esta em uma linha conhecida.
editor.nod_help.guide.window_title=A janela
editor.nod_help.guide.screenshot_caption=Exemplo: um conversor Celsius/Fahrenheit com a dica de comando para <code>Name</code>.
editor.nod_help.guide.create_title=Criar um conversor
editor.nod_help.guide.create.name=Comece com <a class="cmd-link" href="nodpage:cmd:name"><code>Name</code></a>, <a class="cmd-link" href="nodpage:cmd:input1"><code>input1</code></a> e <a class="cmd-link" href="nodpage:cmd:input2"><code>input2</code></a>.
editor.nod_help.guide.create.symbols=Opcionalmente adicione <a class="cmd-link" href="nodpage:cmd:symb1"><code>Symb1</code></a> ate <a class="cmd-link" href="nodpage:cmd:symb4"><code>Symb4</code></a> para simbolos a esquerda ou a direita dos valores.
editor.nod_help.guide.create.rules=Escreva a conversao com <a class="cmd-link" href="nodpage:cmd:math"><code>math</code></a>, <a class="cmd-link" href="nodpage:cmd:trans"><code>trans</code></a> ou <a class="cmd-link" href="nodpage:cmd:chg"><code>chg</code></a>.
editor.nod_help.guide.create.validate=Use <b>Validate</b> para encontrar erros de sintaxe e <b>Test</b> para testar o resultado.
editor.nod_help.guide.create.save=Salve o arquivo quando o conversor estiver correto.
editor.nod_help.guide.tips_title=Dicas de comandos
editor.nod_help.guide.tips=O balao azul mostra ajuda curta para a linha onde o cursor esta. As vezes tambem mostra uma forma mais moderna. Use <b>Replace</b> somente quando voce realmente quiser essa forma moderna; <b>More help</b> abre a pagina completa do comando.
editor.nod_help.guide.undo_title=Desfazer, refazer e restaurar
editor.nod_help.guide.undo=<b>Undo</b> e <b>Redo</b> tratam de edicoes recentes de texto. <b>Restore</b> e maior: retorna a guia atual para a ultima versao aberta ou salva. <b>Tools &gt; Repair lines</b> e diferente; tenta reparar linhas NOD coladas ou mescladas.
formula_card.formula_film=Filme de formulas
formula_card.formula_film_title=Experimento Formula film 2.1
formula_card.formula_film_subtitle=Pesquisa para cartoes de formulas em movimento e filmes didaticos
legal.last_updated.label=Ultima atualizacao:
legal.last_updated.date=21 de maio de 2026
legal.license.title=Contrato de licenca
legal.license.intro=Este contrato de licenca aplica-se ao Syscalculator 2.0, publicado por Edward Tie / Tiedragon.
legal.license.scope.title=Escopo
legal.license.scope.body=Syscalculator e software licenciado. Builds daily e beta sao builds de avaliacao e podem ser alterados, substituidos ou retirados durante o desenvolvimento.
legal.license.daily.title=Builds daily e beta
legal.license.daily.body=Builds daily e beta sao fornecidos para testes, feedback e avaliacao. Eles podem conter recursos inacabados, regressoes ou comportamento diagnostico.
legal.license.use.title=Uso permitido
legal.license.use.personal=Voce pode instalar e usar Syscalculator para trabalho pessoal, testes e aprendizado.
legal.license.use.internal=Organizacoes podem avaliar Syscalculator internamente, salvo acordo escrito separado em contrario.
legal.license.use.no_resale=Voce nao pode vender, alugar, sublicenciar ou redistribuir Syscalculator como seu proprio produto.
legal.license.ownership.title=Propriedade
legal.license.ownership.body=Syscalculator, o nome Tiedragon, codigo do aplicativo, modulos de graficos, conteudo de ajuda e ativos relacionados permanecem propriedade de Edward Tie / Tiedragon, salvo se um arquivo declarar explicitamente outra licenca.
legal.license.warranty.title=Sem garantia
legal.license.warranty.body=Syscalculator e fornecido no estado em que se encontra. Calculos, conversoes e saidas geradas devem ser verificados pelo usuario antes de serem usados para decisoes importantes.
legal.license.liability.title=Limitacao de responsabilidade
legal.license.liability.body=Na maxima extensao permitida por lei, Tiedragon nao se responsabiliza por danos indiretos, perda de dados, interrupcao de negocios ou decisoes tomadas a partir de saidas nao verificadas.
legal.license.third_party.title=Componentes de terceiros
legal.license.third_party.body=Syscalculator pode usar componentes de terceiros como .NET, WebView2, servicos de distribuicao GitHub ou componentes da plataforma Microsoft. Esses componentes continuam sujeitos aos seus proprios termos.
legal.license.contact.title=Contato
legal.license.contact.body=Perguntas sobre licenciamento podem ser enviadas para info@tiedragon.com.
legal.privacy.title=Declaracao de privacidade
legal.privacy.intro=Esta declaracao de privacidade aplica-se ao Syscalculator, publicado por Edward Tie / Tiedragon.
legal.privacy.summary.title=Resumo curto
legal.privacy.summary.body=Syscalculator e um aplicativo de desktop. Calculos, conversoes, arquivos NOD e configuracoes normalmente sao processados localmente no seu computador. Syscalculator nao vende dados pessoais e nao usa publicidade nem rastreamento analitico.
legal.privacy.local.title=Dados locais
legal.privacy.local.body=Syscalculator pode armazenar dados locais necessarios para usar o aplicativo.
legal.privacy.local.settings=Configuracoes do aplicativo.
legal.privacy.local.language=Idioma selecionado e canal de atualizacao.
legal.privacy.local.recent=Arquivos recentes e status de atualizacao.
legal.privacy.local.files=Arquivos NOD que voce cria ou abre.
legal.privacy.feedback.title=Feedback
legal.privacy.feedback.body=Se voce usar o formulario de feedback, as informacoes inseridas serao enviadas para a Tiedragon.
legal.privacy.feedback.type=Tipo de feedback.
legal.privacy.feedback.name=Nome, se voce informar um.
legal.privacy.feedback.email=Endereco de e-mail, se voce informar um.
legal.privacy.feedback.message=Assunto e texto da mensagem.
legal.privacy.feedback.support=Informacoes opcionais de suporte, como versao do produto, canal de lancamento, sistema operacional, versao do .NET e arquitetura do processador.
legal.privacy.updates.title=Atualizacoes
legal.privacy.updates.body=Quando Syscalculator verifica atualizacoes, pode baixar um manifesto de tiedragon.com. O servidor pode receber dados tecnicamente necessarios, como endereco IP, hora da solicitacao e dados padrao de log do servidor.
legal.privacy.use.title=Como os dados sao usados
legal.privacy.use.body=Os dados sao usados para fazer Syscalculator funcionar, salvar configuracoes locais, responder feedback ou suporte, melhorar o software e fornecer atualizacoes.
legal.privacy.sharing.title=Compartilhamento com terceiros
legal.privacy.sharing.body=Tiedragon nao vende dados pessoais. Dados podem ser processados apenas por provedores tecnicos necessarios para hospedagem, e-mail, downloads ou distribuicao de software.
legal.privacy.retention.title=Retencao
legal.privacy.retention.body=Dados locais permanecem ate voce exclui-los ou remover Syscalculator. Mensagens de feedback sao mantidas apenas pelo tempo necessario para suporte, solucao de problemas e administracao.
legal.privacy.choices.title=Suas escolhas
legal.privacy.choices.no_name=Voce pode enviar feedback sem informar um nome.
legal.privacy.choices.support=Voce pode desativar informacoes de suporte no formulario de feedback.
legal.privacy.choices.updates=Voce pode desativar verificacoes automaticas de atualizacao ou mudar o canal de atualizacao.
legal.privacy.choices.local=Voce pode remover arquivos e configuracoes locais por conta propria.
legal.privacy.contact.title=Contato
legal.privacy.contact.body=Para perguntas sobre privacidade ou processamento de dados, contate info@tiedragon.com.
menu.about.help=Ajuda para usuarios
wizard.menu.file=Arquivo
wizard.menu.file.close=Fechar
wizard.menu.extra=Extra
wizard.menu.extra.advanced=Modo avancado
wizard.menu.extra.data=Dados estilo Excel
wizard.menu.extra.debug=Debug da area de transferencia
wizard.advanced.title=Dados WizardExpress
wizard.advanced.copy_tsv=Copiar saida TSV
wizard.mode.basic=Modo basico
wizard.subtitle=Area de transferencia entra, conversao sai, com a sensacao antiga de semaforo.
wizard.copy_now=Copiar agora
wizard.input=Entrada
wizard.output=Saida
wizard.status.caption=Status
'@ -split "`n"
    spa = @'
about.license_text.daily=Syscalculator 2.0 compilacion daily. Licencia de evaluacion interna.
about.license_text.beta=Syscalculator 2.0 compilacion beta. Licencia de evaluacion interna.
about.license_text.stable=Syscalculator 2.0. Software con licencia.
editor.nod_help.guide.intro=Este capitulo explica el trabajo en la ventana del editor. Escribes texto NOD en el centro, usas la barra de herramientas para abrir, guardar, probar y validar, y lees consejos de comandos cuando el cursor esta en una linea conocida.
editor.nod_help.guide.window_title=La ventana
editor.nod_help.guide.screenshot_caption=Ejemplo: un convertidor Celsius/Fahrenheit con el consejo de comando para <code>Name</code>.
editor.nod_help.guide.create_title=Crear un convertidor
editor.nod_help.guide.create.name=Empieza con <a class="cmd-link" href="nodpage:cmd:name"><code>Name</code></a>, <a class="cmd-link" href="nodpage:cmd:input1"><code>input1</code></a> y <a class="cmd-link" href="nodpage:cmd:input2"><code>input2</code></a>.
editor.nod_help.guide.create.symbols=Opcionalmente anade <a class="cmd-link" href="nodpage:cmd:symb1"><code>Symb1</code></a> hasta <a class="cmd-link" href="nodpage:cmd:symb4"><code>Symb4</code></a> para simbolos a la izquierda o derecha de los valores.
editor.nod_help.guide.create.rules=Escribe la conversion con <a class="cmd-link" href="nodpage:cmd:math"><code>math</code></a>, <a class="cmd-link" href="nodpage:cmd:trans"><code>trans</code></a> o <a class="cmd-link" href="nodpage:cmd:chg"><code>chg</code></a>.
editor.nod_help.guide.create.validate=Usa <b>Validate</b> para encontrar errores de sintaxis y <b>Test</b> para probar el resultado.
editor.nod_help.guide.create.save=Guarda el archivo cuando el convertidor sea correcto.
editor.nod_help.guide.tips_title=Consejos de comandos
editor.nod_help.guide.tips=El globo azul muestra ayuda breve para la linea donde esta el cursor. A veces tambien muestra una forma mas moderna. Usa <b>Replace</b> solo cuando realmente quieras esa forma moderna; <b>More help</b> abre la pagina completa del comando.
editor.nod_help.guide.undo_title=Deshacer, rehacer y restaurar
editor.nod_help.guide.undo=<b>Undo</b> y <b>Redo</b> tratan sobre ediciones recientes de texto. <b>Restore</b> es mas amplio: devuelve la pestana actual a la ultima version abierta o guardada. <b>Tools &gt; Repair lines</b> es diferente; intenta reparar lineas NOD pegadas o combinadas.
formula_card.formula_film=Película de formulas
formula_card.formula_film_title=Experimento Formula film 2.1
formula_card.formula_film_subtitle=Investigacion para tarjetas de formulas animadas y peliculas didacticas
legal.last_updated.label=Ultima actualizacion:
legal.last_updated.date=21 de mayo de 2026
legal.license.title=Acuerdo de licencia
legal.license.intro=Este acuerdo de licencia se aplica a Syscalculator 2.0, publicado por Edward Tie / Tiedragon.
legal.license.scope.title=Alcance
legal.license.scope.body=Syscalculator es software con licencia. Las compilaciones daily y beta son compilaciones de evaluacion y pueden cambiarse, sustituirse o retirarse durante el desarrollo.
legal.license.daily.title=Compilaciones daily y beta
legal.license.daily.body=Las compilaciones daily y beta se proporcionan para pruebas, comentarios y evaluacion. Pueden contener funciones sin terminar, regresiones o comportamiento diagnostico.
legal.license.use.title=Uso permitido
legal.license.use.personal=Puedes instalar y usar Syscalculator para trabajo personal, pruebas y aprendizaje.
legal.license.use.internal=Las organizaciones pueden evaluar Syscalculator internamente salvo que un acuerdo escrito separado indique lo contrario.
legal.license.use.no_resale=No puedes vender, alquilar, sublicenciar ni redistribuir Syscalculator como tu propio producto.
legal.license.ownership.title=Propiedad
legal.license.ownership.body=Syscalculator, el nombre Tiedragon, el codigo de la aplicacion, los modulos graficos, el contenido de ayuda y los recursos relacionados siguen siendo propiedad de Edward Tie / Tiedragon salvo que un archivo indique explicitamente otra licencia.
legal.license.warranty.title=Sin garantia
legal.license.warranty.body=Syscalculator se proporciona tal cual. Los calculos, conversiones y salidas generadas deben ser revisados por el usuario antes de usarse para decisiones importantes.
legal.license.liability.title=Limitacion de responsabilidad
legal.license.liability.body=En la maxima medida permitida por la ley, Tiedragon no es responsable de danos indirectos, perdida de datos, interrupcion del negocio o decisiones tomadas a partir de salidas no revisadas.
legal.license.third_party.title=Componentes de terceros
legal.license.third_party.body=Syscalculator puede usar componentes de terceros como .NET, WebView2, servicios de distribucion de GitHub o componentes de plataforma Microsoft. Esos componentes siguen sujetos a sus propios terminos.
legal.license.contact.title=Contacto
legal.license.contact.body=Las preguntas sobre licencias pueden enviarse a info@tiedragon.com.
legal.privacy.title=Declaracion de privacidad
legal.privacy.intro=Esta declaracion de privacidad se aplica a Syscalculator, publicado por Edward Tie / Tiedragon.
legal.privacy.summary.title=Resumen breve
legal.privacy.summary.body=Syscalculator es una aplicacion de escritorio. Los calculos, conversiones, archivos NOD y ajustes normalmente se procesan localmente en tu ordenador. Syscalculator no vende datos personales y no usa publicidad ni seguimiento analitico.
legal.privacy.local.title=Datos locales
legal.privacy.local.body=Syscalculator puede almacenar datos locales necesarios para usar la aplicacion.
legal.privacy.local.settings=Ajustes de la aplicacion.
legal.privacy.local.language=Idioma seleccionado y canal de actualizacion.
legal.privacy.local.recent=Archivos recientes y estado de actualizacion.
legal.privacy.local.files=Archivos NOD que creas o abres.
legal.privacy.feedback.title=Comentarios
legal.privacy.feedback.body=Si usas el formulario de comentarios, la informacion que introduces se envia a Tiedragon.
legal.privacy.feedback.type=Tipo de comentario.
legal.privacy.feedback.name=Nombre, si lo introduces.
legal.privacy.feedback.email=Direccion de e-mail, si la introduces.
legal.privacy.feedback.message=Asunto y texto del mensaje.
legal.privacy.feedback.support=Informacion de soporte opcional como version del producto, canal de publicacion, sistema operativo, version de .NET y arquitectura del procesador.
legal.privacy.updates.title=Actualizaciones
legal.privacy.updates.body=Cuando Syscalculator busca actualizaciones, puede descargar un manifiesto desde tiedragon.com. El servidor puede recibir datos tecnicamente necesarios como direccion IP, hora de solicitud y datos estandar de registro del servidor.
legal.privacy.use.title=Como se usan los datos
legal.privacy.use.body=Los datos se usan para que Syscalculator funcione, guardar ajustes locales, responder a comentarios o soporte, mejorar el software y proporcionar actualizaciones.
legal.privacy.sharing.title=Compartir con terceros
legal.privacy.sharing.body=Tiedragon no vende datos personales. Los datos solo pueden ser procesados por proveedores tecnicos necesarios para alojamiento, e-mail, descargas o distribucion de software.
legal.privacy.retention.title=Conservacion
legal.privacy.retention.body=Los datos locales permanecen hasta que los elimines o quites Syscalculator. Los mensajes de comentarios se conservan solo el tiempo necesario para soporte, resolucion de problemas y administracion.
legal.privacy.choices.title=Tus opciones
legal.privacy.choices.no_name=Puedes enviar comentarios sin introducir un nombre.
legal.privacy.choices.support=Puedes desactivar la informacion de soporte en el formulario de comentarios.
legal.privacy.choices.updates=Puedes desactivar las comprobaciones automaticas de actualizaciones o cambiar el canal de actualizacion.
legal.privacy.choices.local=Puedes eliminar archivos y ajustes locales por tu cuenta.
legal.privacy.contact.title=Contacto
legal.privacy.contact.body=Para preguntas sobre privacidad o procesamiento de datos, contacta con info@tiedragon.com.
menu.about.help=Ayuda para usuarios
wizard.menu.file=Archivo
wizard.menu.file.close=Cerrar
wizard.menu.extra=Extra
wizard.menu.extra.advanced=Modo avanzado
wizard.menu.extra.data=Datos estilo Excel
wizard.menu.extra.debug=Depuracion del portapapeles
wizard.advanced.title=Datos WizardExpress
wizard.advanced.copy_tsv=Copiar salida TSV
wizard.mode.basic=Modo basico
wizard.subtitle=Portapapeles de entrada, conversion de salida, con el estilo antiguo de semaforo.
wizard.copy_now=Copiar ahora
wizard.input=Entrada
wizard.output=Salida
wizard.status.caption=Estado
'@ -split "`n"
    zho = @'
about.update_channel=更新通道:
about.license_text.daily=Syscalculator 2.0 daily 构建。内部评估许可。
about.license_text.beta=Syscalculator 2.0 beta 构建。内部评估许可。
about.license_text.stable=Syscalculator 2.0。授权软件。
editor.tabs.new_right=在右侧新建标签页
editor.tabs.close_all=关闭所有标签页
editor.tabs.close_right=关闭右侧标签页
editor.tabs.close_left=关闭左侧标签页
editor.nod_help.guide.intro=本章说明如何在编辑器窗口中工作。你在中间编写 NOD 文本，使用工具栏打开、保存、测试和验证，并在光标位于已知命令行时阅读命令提示。
editor.nod_help.guide.window_title=窗口
editor.nod_help.guide.screenshot_caption=示例：带有 <code>Name</code> 命令提示的摄氏/华氏转换器。
editor.nod_help.guide.create_title=创建转换器
editor.nod_help.guide.create.name=从 <a class="cmd-link" href="nodpage:cmd:name"><code>Name</code></a>、<a class="cmd-link" href="nodpage:cmd:input1"><code>input1</code></a> 和 <a class="cmd-link" href="nodpage:cmd:input2"><code>input2</code></a> 开始。
editor.nod_help.guide.create.symbols=可选添加 <a class="cmd-link" href="nodpage:cmd:symb1"><code>Symb1</code></a> 到 <a class="cmd-link" href="nodpage:cmd:symb4"><code>Symb4</code></a>，用于数值左侧或右侧的符号。
editor.nod_help.guide.create.rules=使用 <a class="cmd-link" href="nodpage:cmd:math"><code>math</code></a>、<a class="cmd-link" href="nodpage:cmd:trans"><code>trans</code></a> 或 <a class="cmd-link" href="nodpage:cmd:chg"><code>chg</code></a> 编写转换。
editor.nod_help.guide.create.validate=使用 <b>Validate</b> 查找语法错误，使用 <b>Test</b> 试运行结果。
editor.nod_help.guide.create.save=转换器正确后保存文件。
editor.nod_help.guide.tips_title=命令提示
editor.nod_help.guide.tips=蓝色气泡会为光标所在行显示简短帮助。有时也会显示更现代的写法。只有在确实需要该现代写法时才使用 <b>Replace</b>；<b>More help</b> 会打开完整命令页面。
editor.nod_help.guide.undo_title=撤销、重做和恢复
editor.nod_help.guide.undo=<b>Undo</b> 和 <b>Redo</b> 用于最近的文本编辑。<b>Restore</b> 范围更大：它把当前标签页恢复到最后打开或保存的版本。<b>Tools &gt; Repair lines</b> 不同；它会尝试修复粘贴或合并的 NOD 行。
formula_card.formula_film=公式影片
formula_card.formula_film_title=公式影片 2.1 实验
formula_card.formula_film_subtitle=用于动态公式卡和教学影片的研究
legal.last_updated.label=最后更新:
legal.last_updated.date=2026年5月21日
legal.license.title=许可协议
legal.license.intro=本许可协议适用于 Edward Tie / Tiedragon 发布的 Syscalculator 2.0。
legal.license.scope.title=范围
legal.license.scope.body=Syscalculator 是授权软件。Daily 和 beta 构建是评估版本，在开发期间可能被修改、替换或撤回。
legal.license.daily.title=Daily 和 Beta 构建
legal.license.daily.body=Daily 和 beta 构建用于测试、反馈和评估。它们可能包含未完成的功能、回归问题或诊断行为。
legal.license.use.title=允许的使用
legal.license.use.personal=你可以安装并使用 Syscalculator 进行个人工作、测试和学习。
legal.license.use.internal=除非另有单独书面协议，组织可以在内部评估 Syscalculator。
legal.license.use.no_resale=你不得将 Syscalculator 作为自己的产品出售、出租、再授权或重新分发。
legal.license.ownership.title=所有权
legal.license.ownership.body=除非文件明确声明其他许可，Syscalculator、Tiedragon 名称、应用代码、图形模块、帮助内容和相关资源仍归 Edward Tie / Tiedragon 所有。
legal.license.warranty.title=无担保
legal.license.warranty.body=Syscalculator 按现状提供。计算、转换和生成的输出在用于重要决定前应由用户检查。
legal.license.liability.title=责任限制
legal.license.liability.body=在法律允许的最大范围内，Tiedragon 不对间接损害、数据丢失、业务中断或基于未检查输出所作决定承担责任。
legal.license.third_party.title=第三方组件
legal.license.third_party.body=Syscalculator 可能使用第三方组件，例如 .NET、WebView2、GitHub 分发服务或 Microsoft 平台组件。这些组件仍受其自身条款约束。
legal.license.contact.title=联系
legal.license.contact.body=许可问题可发送至 info@tiedragon.com。
legal.privacy.title=隐私声明
legal.privacy.intro=本隐私声明适用于 Edward Tie / Tiedragon 发布的 Syscalculator。
legal.privacy.summary.title=简短摘要
legal.privacy.summary.body=Syscalculator 是桌面应用。计算、转换、NOD 文件和设置通常在你的计算机本地处理。Syscalculator 不出售个人数据，也不使用广告或分析跟踪。
legal.privacy.local.title=本地数据
legal.privacy.local.body=Syscalculator 可能存储使用应用所需的本地数据。
legal.privacy.local.settings=应用设置。
legal.privacy.local.language=所选语言和更新通道。
legal.privacy.local.recent=最近文件和更新状态。
legal.privacy.local.files=你创建或打开的 NOD 文件。
legal.privacy.feedback.title=反馈
legal.privacy.feedback.body=如果你使用反馈表单，输入的信息会发送给 Tiedragon。
legal.privacy.feedback.type=反馈类型。
legal.privacy.feedback.name=姓名，如果你填写。
legal.privacy.feedback.email=电子邮件地址，如果你填写。
legal.privacy.feedback.message=主题和消息文本。
legal.privacy.feedback.support=可选支持信息，例如产品版本、发布通道、操作系统、.NET 版本和处理器架构。
legal.privacy.updates.title=更新
legal.privacy.updates.body=当 Syscalculator 检查更新时，可能会从 tiedragon.com 下载更新清单。服务器可能接收技术上必要的数据，例如 IP 地址、请求时间和标准服务器日志数据。
legal.privacy.use.title=数据如何使用
legal.privacy.use.body=数据用于让 Syscalculator 工作、保存本地设置、回复反馈或支持请求、改进软件并提供更新。
legal.privacy.sharing.title=与第三方共享
legal.privacy.sharing.body=Tiedragon 不出售个人数据。数据只能由托管、电子邮件、下载或软件分发所需的技术服务提供商处理。
legal.privacy.retention.title=保留
legal.privacy.retention.body=本地数据会保留到你删除它或移除 Syscalculator。反馈消息仅在支持、故障排除和管理所必需的期间保留。
legal.privacy.choices.title=你的选择
legal.privacy.choices.no_name=你可以不输入姓名就发送反馈。
legal.privacy.choices.support=你可以在反馈表单中关闭支持信息。
legal.privacy.choices.updates=你可以禁用自动更新检查或更改更新通道。
legal.privacy.choices.local=你可以自行删除本地文件和设置。
legal.privacy.contact.title=联系
legal.privacy.contact.body=如有隐私或数据处理问题，请联系 info@tiedragon.com。
menu.about.help=用户帮助
menu.about.language_manager=语言管理器...
menu.about.check_updates=检查更新...
menu.config.auto_update_check=自动检查更新
menu.config.update_channel=更新通道
menu.config.update_channel.daily=Daily
menu.config.update_channel.beta=Beta
status.auto_update_check_on=已开启自动更新检查。
status.auto_update_check_off=已关闭自动更新检查。
status.update_channel_changed=更新通道: {0}。
update.title=Syscalculator 更新
update.checking=正在检查更新...
update.none=此通道已是最新版本。
update.none_status=Syscalculator 已是最新。
update.failed=无法检查更新。\n\n{0}
update.failed_status=更新检查失败。
update.available_title=有新的 Syscalculator 版本
update.available={0}\n\n版本: {1}\n日期: {2}{3}\n\n下载此更新?
update.download_opened=已打开更新下载。
update.updater_started=更新程序已启动。
update.later=更新已推迟。
wizard.menu.file=文件
wizard.menu.file.close=关闭
wizard.menu.extra=额外
wizard.menu.extra.advanced=高级模式
wizard.menu.extra.data=Excel 风格数据
wizard.menu.extra.debug=剪贴板调试
wizard.advanced.title=WizardExpress 数据
wizard.advanced.copy_tsv=复制输出 TSV
wizard.mode.basic=基本模式
wizard.subtitle=剪贴板输入，转换输出，保留旧式信号灯感觉。
wizard.copy_now=立即复制
wizard.input=输入
wizard.output=输出
wizard.status.caption=状态
'@ -split "`n"
}

foreach ($entry in $sharedKeys.GetEnumerator()) {
    Add-MissingLanguageLines -Code $entry.Key -Lines $entry.Value
}
