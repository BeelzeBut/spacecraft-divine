UNITY := /Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity
PROJECT := $(shell pwd)
LOGDIR := build-logs

.PHONY: guard test android-aab android-apk ios clean-logs

# Batchmode are nevoie de lacatul proiectului: un editor deschis blocheaza orice tinta.
guard:
	@if pgrep -fl "Unity.app/Contents/MacOS/Unity" >/dev/null 2>&1; then \
		echo "Un editor Unity este deschis. Inchide-l intai."; exit 1; \
	fi
	@mkdir -p $(LOGDIR)

test: guard
	./run-tests.sh

# Codul de iesire este al lui Unity, nu al lui tail. Un "| tail" simplu ar raporta
# starea lui tail si ar transforma orice compilare esuata in succes aparent - exact
# defectul pe care punctele de intrare il evita prin Exit(1).
android-aab: guard
	@"$(UNITY)" -batchmode -quit -projectPath "$(PROJECT)" \
	  -executeMethod BuildAndroid.BuildAab -logFile $(LOGDIR)/android-aab.log; \
	  status=$$?; tail -30 $(LOGDIR)/android-aab.log; exit $$status

android-apk: guard
	@"$(UNITY)" -batchmode -quit -projectPath "$(PROJECT)" \
	  -executeMethod BuildAndroid.BuildApk -logFile $(LOGDIR)/android-apk.log; \
	  status=$$?; tail -30 $(LOGDIR)/android-apk.log; exit $$status

ios: guard
	@"$(UNITY)" -batchmode -quit -projectPath "$(PROJECT)" \
	  -executeMethod BuildIOS.BuildXcodeProject -logFile $(LOGDIR)/ios.log; \
	  status=$$?; tail -30 $(LOGDIR)/ios.log; exit $$status

clean-logs:
	rm -rf $(LOGDIR)
