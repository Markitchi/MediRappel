#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  MediRappel — utilitaire émulateur Android
#
#  Usage :
#    bash tools/emulateur.sh --verifier   # état du SDK et de l'appareil
#    bash tools/emulateur.sh --creer      # crée l'AVD MediRappel_API36
#    bash tools/emulateur.sh --demarrer   # démarre l'émulateur
#    bash tools/emulateur.sh --arreter    # arrête l'émulateur
#    bash tools/emulateur.sh --lancer     # compile, installe et lance l'app
# ---------------------------------------------------------------------------
set -euo pipefail

SDK="${ANDROID_HOME:-$HOME/android-sdk}"
export ANDROID_HOME="$SDK"
export ANDROID_SDK_ROOT="$SDK"
export JAVA_HOME="${JAVA_HOME:-$HOME/.jdk}"
export PATH="$JAVA_HOME/bin:$SDK/platform-tools:$SDK/emulator:$SDK/cmdline-tools/latest/bin:$PATH"

IMAGE="system-images;android-36;google_apis;x86_64"
AVD="MediRappel_API36"
APP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../MediRappel/App" && pwd)"

# Réglages de performance (machine cible : 4 cœurs / 8 threads, 30 Go de RAM,
# GPU Intel UHD 630). « gpu host » délègue le rendu au GPU de la machine au lieu
# de le simuler en logiciel : c'est ce qui supprime la lenteur de l'émulateur.
EMU_FLAGS=(-gpu host -memory 8192 -cores 8
           -no-snapshot-load -no-snapshot-save
           -no-boot-anim -netdelay none -netspeed full)
EMU_SER="emulator-5554"

log() { printf '\033[36m▸\033[0m %s\n' "$1"; }
ok()  { printf '\033[32m✓\033[0m %s\n' "$1"; }
ko()  { printf '\033[31m✗\033[0m %s\n' "$1" >&2; }

verifier() {
  log "SDK Android : $SDK"
  [ -x "$SDK/platform-tools/adb" ] && ok "adb présent" || ko "adb manquant (sdkmanager platform-tools)"
  [ -x "$SDK/emulator/emulator" ] && ok "émulateur présent" || ko "émulateur manquant (sdkmanager emulator)"
  if [ -d "$SDK/system-images/android-36/google_apis/x86_64" ]; then
    ok "image système android-36 (google_apis, x86_64) installée"
  else
    ko "image système absente : $IMAGE"
  fi
  echo
  log "Appareils connectés"
  adb devices -l | sed '1d' | grep . || echo "  (aucun)"
}

creer() {
  if "$SDK/emulator/emulator" -list-avds | grep -qx "$AVD"; then
    ok "AVD « $AVD » existe déjà"
    return
  fi
  log "Création de l'AVD « $AVD » (Pixel 6, API 36)"
  echo "no" | avdmanager create avd \
      -n "$AVD" \
      -k "$IMAGE" \
      -d "pixel_6" \
      --force
  ok "AVD créé : $AVD"
}

demarrer() {
  if adb devices | grep -q "emulator-.*device"; then
    ok "un émulateur est déjà démarré"
    return
  fi
  log "Démarrage de l'émulateur (gpu host, 4 Go de RAM, 4 cœurs)…"
  nohup "$SDK/emulator/emulator" -avd "$AVD" "${EMU_FLAGS[@]}" \
      >/tmp/medirappel-emulateur.log 2>&1 &
  log "Attente de l'appareil…"
  until [ "$(adb -s "$EMU_SER" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = "1" ]; do
    sleep 2
  done
  ok "Émulateur prêt (journal : /tmp/medirappel-emulateur.log)"
}

arreter() {
  log "Arrêt de l'émulateur"
  adb -s emulator-5554 emu kill 2>/dev/null || pkill -f "emulator -avd $AVD" || true
  ok "Émulateur arrêté"
}

lancer() {
  log "Compilation et déploiement sur l'émulateur"
  # On cible explicitement MediRappel.csproj : la solution .slnx inclut aussi
  # l'API, qui ne possède pas de cible net10.0-android.
  ( cd "$APP_DIR" && dotnet build MediRappel.csproj -t:Run -f net10.0-android -c Debug \
      -p:AdbTarget="-s $EMU_SER" )
  ok "Application déployée"
}

case "${1:---verifier}" in
  --verifier) verifier ;;
  --creer)    creer ;;
  --demarrer) demarrer ;;
  --arreter)  arreter ;;
  --lancer)   lancer ;;
  -h|--help)  sed -n '3,12p' "$0" | sed 's/^# \{0,1\}//' ;;
  *) ko "option inconnue : $1"; exit 1 ;;
esac
