#import <AVFoundation/AVFoundation.h>

// Sesli konuşma için ses oturumu: playAndRecord + voiceChat, varsayılan çıkış ana hoparlör.
// Kulaklık/Bluetooth takılıysa iOS onu seçmeye devam eder (defaultToSpeaker yalnızca ahizeyi hoparlöre çevirir).
//
// Mikrofon başlarken Unity/Photon oturumu yeniden kurabilir ve ahizeye düşebilir; bu yüzden C# tarafı mikrofon
// başladıktan sonra ve her rota değişiminde _BlofAudioReapply çağırır. Rota değişimleri sayılır
// (_BlofAudioRouteChangeCount): sayı değişince C# yeniden uygular.

static int gRouteChanges = 0;
static BOOL gObserving = NO;
static BOOL gInterrupted = NO;   // arama/Siri kesintisi sürüyor (began..ended)

static void BlofEnsureObserver()
{
    if (gObserving) return;
    gObserving = YES;
    [[NSNotificationCenter defaultCenter] addObserverForName:AVAudioSessionRouteChangeNotification
                                                      object:nil
                                                       queue:nil
                                                  usingBlock:^(NSNotification *note) { gRouteChanges++; }];
    // Kesinti (arama, Siri) bitince oturum sıfırlanır: bunu da rota değişimi say.
    [[NSNotificationCenter defaultCenter] addObserverForName:AVAudioSessionInterruptionNotification
                                                      object:nil
                                                       queue:nil
                                                  usingBlock:^(NSNotification *note) {
        NSNumber *type = note.userInfo[AVAudioSessionInterruptionTypeKey];
        gInterrupted = (type != nil && type.unsignedIntegerValue == AVAudioSessionInterruptionTypeBegan);
        gRouteChanges++;
    }];
}

// Çıkışta kulaklık / Bluetooth / USB / HDMI gibi harici bir rota var mı?
static BOOL BlofExternalOutput()
{
    for (AVAudioSessionPortDescription *out in [AVAudioSession sharedInstance].currentRoute.outputs)
    {
        NSString *t = out.portType;
        if ([t isEqualToString:AVAudioSessionPortHeadphones] ||
            [t isEqualToString:AVAudioSessionPortBluetoothA2DP] ||
            [t isEqualToString:AVAudioSessionPortBluetoothHFP] ||
            [t isEqualToString:AVAudioSessionPortBluetoothLE] ||
            [t isEqualToString:AVAudioSessionPortUSBAudio] ||
            [t isEqualToString:AVAudioSessionPortHDMI] ||
            [t isEqualToString:AVAudioSessionPortAirPlay] ||
            [t isEqualToString:AVAudioSessionPortCarAudio])
            return YES;
    }
    return NO;
}

static void BlofApplySpeaker()
{
    AVAudioSession *session = [AVAudioSession sharedInstance];
    NSError *error = nil;
    if (BlofExternalOutput())
    {
        [session overrideOutputAudioPort:AVAudioSessionPortOverrideNone error:&error];
    }
    else
    {
        [session overrideOutputAudioPort:AVAudioSessionPortOverrideSpeaker error:&error];
    }
    if (error != nil) NSLog(@"BlofAudioRoute: cikis yonlendirilemedi: %@", error);
}

extern "C" void _BlofAudioEnterVoiceChat()
{
    BlofEnsureObserver();
    AVAudioSession *session = [AVAudioSession sharedInstance];
    NSError *error = nil;
    [session setCategory:AVAudioSessionCategoryPlayAndRecord
                    mode:AVAudioSessionModeVoiceChat
                 options:AVAudioSessionCategoryOptionDefaultToSpeaker | AVAudioSessionCategoryOptionAllowBluetooth
                   error:&error];
    if (error != nil) { NSLog(@"BlofAudioRoute: kategori ayarlanamadi: %@", error); return; }
    [session setActive:YES error:&error];
    if (error != nil) NSLog(@"BlofAudioRoute: oturum acilamadi: %@", error);
    BlofApplySpeaker();
}

// Mikrofon başladıktan / rota değiştikten sonra: kategori başkası tarafından değiştirildiyse geri kur, hoparlöre al.
extern "C" void _BlofAudioReapply()
{
    AVAudioSession *session = [AVAudioSession sharedInstance];
    BOOL ok = [session.category isEqualToString:AVAudioSessionCategoryPlayAndRecord] &&
              (session.categoryOptions & AVAudioSessionCategoryOptionDefaultToSpeaker) != 0;
    if (!ok)
    {
        NSError *error = nil;
        [session setCategory:AVAudioSessionCategoryPlayAndRecord
                        mode:AVAudioSessionModeVoiceChat
                     options:AVAudioSessionCategoryOptionDefaultToSpeaker | AVAudioSessionCategoryOptionAllowBluetooth
                       error:&error];
        if (error != nil) NSLog(@"BlofAudioRoute: kategori yeniden kurulamadi: %@", error);
        [session setActive:YES error:&error];
    }
    BlofApplySpeaker();
}

extern "C" int _BlofAudioInterrupted()
{
    BlofEnsureObserver();
    return gInterrupted ? 1 : 0;
}

extern "C" int _BlofAudioRouteChangeCount()
{
    BlofEnsureObserver();
    return gRouteChanges;
}

// Ses kapanınca Unity'nin varsayılanına (Ambient) dön.
extern "C" void _BlofAudioLeaveVoiceChat()
{
    AVAudioSession *session = [AVAudioSession sharedInstance];
    NSError *error = nil;
    [session overrideOutputAudioPort:AVAudioSessionPortOverrideNone error:&error];
    [session setCategory:AVAudioSessionCategoryAmbient mode:AVAudioSessionModeDefault options:0 error:&error];
    if (error != nil) NSLog(@"BlofAudioRoute: eski kategoriye donulemedi: %@", error);
}
