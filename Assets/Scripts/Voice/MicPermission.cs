using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace CoopGame
{
    /// <summary>Mikrofon izni (Android RECORD_AUDIO, iOS NSMicrophoneUsageDescription). blofapp VoiceService izin akışından uyarlandı.</summary>
    public static class MicPermission
    {
        public static IEnumerator Ask(Action<bool> done)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                bool answered = false, ok = false;
                var cb = new PermissionCallbacks();
                cb.PermissionGranted += _ => { ok = true; answered = true; };
                cb.PermissionDenied += _ => answered = true;
                cb.PermissionDeniedAndDontAskAgain += _ => answered = true;
                Permission.RequestUserPermission(Permission.Microphone, cb);
                float t = 0f;
                while (!answered && t < 60f) { t += Time.unscaledDeltaTime; yield return null; }
                done(ok);
                yield break;
            }
            done(true);
#elif UNITY_IOS && !UNITY_EDITOR
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            done(Application.HasUserAuthorization(UserAuthorization.Microphone));
#else
            done(Microphone.devices.Length > 0);
            yield break;
#endif
        }
    }
}
