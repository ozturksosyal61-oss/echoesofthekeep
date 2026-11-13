#import <GameKit/GameKit.h>
#import <UIKit/UIKit.h>

// Game Center köprüsü: sessiz giriş, arkadaş daveti (GKMatchmakerViewController), oda kodu aktarımı.
// Akış: davet eden (host) eşleşince C#'a "host" der → C# Photon odasını kurar → _CoopGCSendCode(kod) → GKMatch ile
// davetliye gider → davetli "ack" yollar ve C#'a kodu verir → iki taraf GKMatch'ten çıkar, oyun Photon'da sürer.
// C# alıcı: "GameCenter" adlı GameObject (GameCenter.cs). Mesajlar: OnAuth("1|ad" / "0|hata"), OnMatch("host" / "guest"),
// OnCode("1234"), OnMatchFail("neden").

extern UIViewController *UnityGetGLViewController(void);
extern void UnitySendMessage(const char *obj, const char *method, const char *msg);

static void Send(const char *method, NSString *msg) { UnitySendMessage("GameCenter", method, msg.UTF8String ?: ""); }

@interface CoopGCDelegate : NSObject <GKMatchmakerViewControllerDelegate, GKMatchDelegate, GKLocalPlayerListener>
@property (nonatomic, strong) GKMatch *match;
@property (nonatomic) BOOL isHost;
@property (nonatomic) BOOL announced;
@end

static CoopGCDelegate *gDelegate;
static BOOL gAuthStarted;

@implementation CoopGCDelegate

- (void)present:(GKMatchmakerViewController *)vc
{
    if (vc == nil) { Send("OnMatchFail", @"ui"); return; }
    vc.matchmakerDelegate = self;
    [UnityGetGLViewController() presentViewController:vc animated:YES completion:nil];
}

- (void)matchmakerViewControllerWasCancelled:(GKMatchmakerViewController *)vc
{
    [vc dismissViewControllerAnimated:YES completion:nil];
    Send("OnMatchFail", @"cancel");
}

- (void)matchmakerViewController:(GKMatchmakerViewController *)vc didFailWithError:(NSError *)error
{
    [vc dismissViewControllerAnimated:YES completion:nil];
    Send("OnMatchFail", error.localizedDescription ?: @"error");
}

- (void)matchmakerViewController:(GKMatchmakerViewController *)vc didFindMatch:(GKMatch *)match
{
    [vc dismissViewControllerAnimated:YES completion:nil];
    self.match = match;
    match.delegate = self;
    self.announced = NO;
    [self checkReady];
}

- (void)checkReady
{
    if (self.match == nil || self.announced || self.match.expectedPlayerCount > 0) return;
    self.announced = YES;
    Send("OnMatch", self.isHost ? @"host" : @"guest");
}

- (void)match:(GKMatch *)match player:(GKPlayer *)player didChangeConnectionState:(GKPlayerConnectionState)state
{
    if (state == GKPlayerStateConnected) [self checkReady];
    else if (state == GKPlayerStateDisconnected && !self.announced) Send("OnMatchFail", @"disconnect");
}

- (void)match:(GKMatch *)match didReceiveData:(NSData *)data fromRemotePlayer:(GKPlayer *)player
{
    NSString *s = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] ?: @"";
    if ([s isEqualToString:@"ack"]) { [self end]; return; }    // host: davetli kodu aldı
    if ([s hasPrefix:@"code:"])
    {
        [match sendDataToAllPlayers:[@"ack" dataUsingEncoding:NSUTF8StringEncoding] withDataMode:GKMatchSendDataReliable error:nil];
        Send("OnCode", [s substringFromIndex:5]);
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(1.5 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{ [self end]; });
    }
}

- (void)end
{
    [self.match disconnect];
    self.match.delegate = nil;
    self.match = nil;
}

// Davet kabulü (bildirimden ya da uygulama kapalıyken açılış): davetli tarafı
- (void)player:(GKPlayer *)player didAcceptInvite:(GKInvite *)invite
{
    self.isHost = NO;
    [self present:[[GKMatchmakerViewController alloc] initWithInvite:invite]];
}

@end

extern "C"
{
    void _CoopGCAuthenticate()
    {
        if (gDelegate == nil) gDelegate = [CoopGCDelegate new];
        if (gAuthStarted) return;
        gAuthStarted = YES;
        GKLocalPlayer *lp = GKLocalPlayer.localPlayer;
        lp.authenticateHandler = ^(UIViewController *vc, NSError *error) {
            if (vc != nil) { [UnityGetGLViewController() presentViewController:vc animated:YES completion:nil]; return; }
            if (lp.isAuthenticated)
            {
                [lp registerListener:gDelegate];
                Send("OnAuth", [@"1|" stringByAppendingString:lp.displayName ?: @""]);
            }
            else Send("OnAuth", [@"0|" stringByAppendingString:error.localizedDescription ?: @""]);
        };
    }

    int _CoopGCIsAuthenticated() { return GKLocalPlayer.localPlayer.isAuthenticated ? 1 : 0; }

    void _CoopGCInvite()
    {
        if (!GKLocalPlayer.localPlayer.isAuthenticated) { Send("OnMatchFail", @"auth"); return; }
        GKMatchRequest *req = [GKMatchRequest new];
        req.minPlayers = 2; req.maxPlayers = 2;
        req.inviteMessage = @"Şatoda beni bekle: Echoes of the Keep";
        gDelegate.isHost = YES;
        GKMatchmakerViewController *vc = [[GKMatchmakerViewController alloc] initWithMatchRequest:req];
        vc.matchmakingMode = GKMatchmakingModeInviteOnly;
        [gDelegate present:vc];
    }

    void _CoopGCSendCode(const char *code)
    {
        if (gDelegate.match == nil) return;
        NSString *s = [@"code:" stringByAppendingString:[NSString stringWithUTF8String:code]];
        [gDelegate.match sendDataToAllPlayers:[s dataUsingEncoding:NSUTF8StringEncoding] withDataMode:GKMatchSendDataReliable error:nil];
        // Güvence: ack gelmezse 15 sn sonra eşleşmeyi kapat
        dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(15 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{ if (gDelegate.match != nil) [gDelegate end]; });
    }

    void _CoopGCCancelMatch() { if (gDelegate.match != nil) [gDelegate end]; }

    void _CoopGCShowDashboard()
    {
        if (@available(iOS 14.0, *)) [GKAccessPoint.shared triggerAccessPointWithHandler:^{}];
    }
}
