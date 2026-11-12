#!/bin/bash
# Echoes of the Keep → TestFlight (Mac). Tek komut: güncel kodu çeker, Unity ile Xcode projesi üretir, arşivler ve
# App Store Connect'e yükler (Xcode'da oturum açık Apple hesabı kullanılır; Transporter gerekmez).
#
# Kurulum (bir kez): Unity Hub'da 6000.6.7f1 + "iOS Build Support"; Xcode → Settings → Accounts'ta Apple ID;
# App Store Connect'te uygulama kaydı (BUNDLE_ID ile).
# Kullanım:  TEAM_ID=ABCDE12345 BUNDLE_ID=com.ornek.echoes ./Tools/ios_testflight.sh
set -euo pipefail

: "${TEAM_ID:?TEAM_ID gerekli (developer.apple.com → Membership → Team ID)}"
: "${BUNDLE_ID:?BUNDLE_ID gerekli (App Store Connect'teki paket kimliği)}"
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/Builds"
cd "$ROOT"

echo "== git pull"
git pull --ff-only

echo "== Unity: Xcode projesi (ilk seferde içe aktarma uzun sürer)"
mkdir -p Logs
"$UNITY" -batchmode -quit -projectPath "$ROOT" -buildTarget iOS \
  -executeMethod CoopGame.EditorTools.CoopSetup.BuildIOS -bundleId "$BUNDLE_ID" -teamId "$TEAM_ID" \
  -logFile "$ROOT/Logs/ios_build.log" || { echo "Unity build başarısız: Logs/ios_build.log"; exit 1; }

echo "== Xcode: arşiv"
rm -rf "$OUT/EchoesOfTheKeep.xcarchive" "$OUT/ios_export"
xcodebuild -project "$OUT/iOS/Unity-iPhone.xcodeproj" -scheme Unity-iPhone -configuration Release \
  -archivePath "$OUT/EchoesOfTheKeep.xcarchive" -destination "generic/platform=iOS" \
  -allowProvisioningUpdates DEVELOPMENT_TEAM="$TEAM_ID" CODE_SIGN_STYLE=Automatic archive | tail -n 20

echo "== Yükleme: App Store Connect (TestFlight)"
cat > "$OUT/ExportOptions.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>method</key><string>app-store-connect</string>
  <key>destination</key><string>upload</string>
  <key>teamID</key><string>$TEAM_ID</string>
  <key>signingStyle</key><string>automatic</string>
  <key>uploadSymbols</key><true/>
</dict></plist>
EOF
xcodebuild -exportArchive -archivePath "$OUT/EchoesOfTheKeep.xcarchive" -exportOptionsPlist "$OUT/ExportOptions.plist" \
  -exportPath "$OUT/ios_export" -allowProvisioningUpdates | tail -n 20

echo "== Tamam: birkaç dakika içinde App Store Connect → TestFlight'ta işlenir."
