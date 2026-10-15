"""Package the local test delta, never install or publish it."""
import hashlib
import json
import zipfile
from pathlib import Path
from project import BASE, OUTPUT, valheim_dir
from transformer_locomotion_review import reviewed,configure

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    manifest=json.loads((OUTPUT/'mod/transformer/transformer.json').read_text())
    walking=manifest['walkingReady']
    if walking:
        assert reviewed() and manifest.get('gaitReviewSha256')==reviewed(), 'Current complete walking review required'
        assert manifest['gaitDefinitionSha256']==digest(BASE/'tools/transformer_gait_v2.json')
        assert manifest['gaitReleases']==configure(dict(manifest))['gaitReleases'], 'Gait release table differs from reviewed settings'
    elif 'gaitClearanceSha256' in manifest:
        assert manifest['gaitClearanceSha256']==digest(BASE/'tools/transformer_gait_clearance.json')
    assert digest(OUTPUT/'mod/body.json')==manifest['bodySha256']
    assert digest(OUTPUT/'mod/transformer/transformer.bin')==manifest['payloadSha256']
    out=OUTPUT/'transformer-prototype';out.mkdir(parents=True,exist_ok=True)
    readme='''NFVU2 - lokaler Transformer-Teststand

Noch NICHT im Spiel freigegeben und NICHT automatisch installiert.
Die Scratch-Notizen wurden nicht umgesetzt.

Enthalten: Rachel350Z.dll sowie transformer/transformer.json und transformer.bin.
Aktiver Umfang: Verwandlung, Standmodus mit Fahrer am Nacken, Kamera und
Rueckverwandlung. GEHEN UND DREHEN SIND GESPERRT: Die Laufkandidaten zeigen noch
Ueberschneidungen an der Beinpanzerung. Die Bewegung bleibt weitere Arbeit.
Dies ist eine Ergaenzung zur bestehenden vollstaendigen Mod, kein Installer.
Die Mod-ID bleibt gleich. Der neue Modus ist standardmaessig ausgeschaltet.

Nach einer gesicherten Testinstallation kann in der BepInEx-Konfiguration
local.rachel350z.prototype.cfg folgender Abschnitt aktiviert werden:
[TransformerPrototype]
Enabled = true
ToggleKey = F10

Einzelspieler: 350Z besteigen, Einstieg abschliessen, Klappen schliessen,
auf einer freien ebenen Flaeche anhalten. F10 verwandelt/zurueck.
Maus bzw. Gamepad RS bewegen die Kamera. Noch keine Roboter-Fortbewegung.
F7 verlangt Rueckverwandlung und Ausstieg; F8 entfernt das Fahrzeug.

Valheim muss fuer einen Dateiaustausch geschlossen sein. Bestehende Mod samt
Konfiguration vorher sichern; Kopierpruefsummen vergleichen. Diese ZIP fuehrt
keine Installation durch. Rueckkehr: gesicherte DLL wiederherstellen,
TransformerPrototype deaktivieren; die vorhandenen Modell-/Audiopakete behalten.

Pruefungen: Managed-Tests, Exportgleichheit, geometrischer Laufcheck.
NICHT geprueft: Unity-Skinning/Physik, echter Avatar, Kamera und Performance
im Spiel. Vorschau unter artifacts/transformer-runtime-review zeigt Blender.
Details: docs/TRANSFORMER-PROTOTYPE.md im Projekt.
'''
    if walking:
        readme=readme.replace('GEHEN UND DREHEN SIND GESPERRT: Die Laufkandidaten zeigen noch\nUeberschneidungen an der Beinpanzerung. Die Bewegung bleibt weitere Arbeit.',
            'GEHEN UND DREHEN: W/S vor und zurueck, A/D drehen; am Gamepad\nRT/LT und linker Stick. Vor dem ersten Schritt oeffnet die Kniepanzerung.\nF10/F7 bremst, setzt die Fuesse ab und schliesst die Panzerung vor der Rueckkehr.\nNur freie ebene Flaechen; Stufen, Abhaenge und Wasser sind gesperrt.')
        readme=readme.replace('Noch keine Roboter-Fortbewegung.','Langsamer Gehmodus und schrittweises Drehen.')
        readme=readme.replace('Langsamer Gehmodus und schrittweises Drehen.',
            'W halten: sanft beschleunigen, laengere Schritte und groesserer Armschwung.\nV2 erreicht nach etwa drei Sekunden 2,1 m/s; rueckwaerts maximal 1,4 m/s.\nBeim Loslassen wird staerker gebremst; Richtungswechsel zuerst bis zum Stillstand.')
    (out/'LIESMICH.txt').write_text(readme,encoding='utf-8')
    files=[Path('Rachel350Z.dll'),Path('transformer/transformer.json'),Path('transformer/transformer.bin')]
    package=out/'NFVU2-Transformer-Prototype.zip'
    with zipfile.ZipFile(package,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
        for relative in files:archive.write(OUTPUT/'mod'/relative,str(relative).replace('\\','/'))
        archive.writestr('LIESMICH.txt',readme)
    installed=valheim_dir()/'BepInEx/plugins/Rachel350Z/body.json'
    report=dict(sourceBodySha256=manifest['bodySha256'],installedBodyMatches=installed.exists() and digest(installed)==manifest['bodySha256'],
        files={str(p):digest(OUTPUT/'mod'/p) for p in files},packageSha256=digest(package),
        renderBatches=manifest['batches'],vertices=manifest['vertices'],animationSamples=manifest['frames'],
        gaitChecksPassed=walking,walkingReady=walking,gaitVersion=manifest.get('gaitVersion',0),
        unityRuntimeTested=False,installed=False,enabledByDefault=False)
    (out/'build-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    (out/'SHA256.txt').write_text(digest(package)+'  '+package.name+'\n',encoding='utf-8')
    print(package)

if __name__=='__main__':main()
