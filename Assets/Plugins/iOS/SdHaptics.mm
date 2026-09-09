#import <UIKit/UIKit.h>

// Generators are cached and prepared. Creating one per call adds latency, and UIKit warns that
// an unprepared generator may drop the first pulse — which on a Confirm or Reject is exactly the
// one the player is waiting to feel.
static UIImpactFeedbackGenerator *lightGen  = nil;
static UIImpactFeedbackGenerator *mediumGen = nil;
static UIImpactFeedbackGenerator *heavyGen  = nil;
static UINotificationFeedbackGenerator *noticeGen = nil;
static UISelectionFeedbackGenerator *selectGen = nil;

extern "C" {

bool _SdHapticsAvailable() {
    // UIFeedbackGenerator exists from iOS 10. Devices older than iPhone 7 have no Taptic Engine
    // and the calls become silent no-ops, which is the correct degradation.
    if (@available(iOS 10.0, *)) { return true; }
    return false;
}

// Allocation only. `prepare` is deliberately NOT called here for all five: prepare powers up
// the Taptic Engine and holds it in a high-power state for about a second, so preparing every
// generator on every pulse would keep the engine continuously warm during combat — precisely
// the battery drain the rate limiter exists to avoid.
static void EnsureAllocated() {
    if (@available(iOS 10.0, *)) {
        if (lightGen  == nil) lightGen  = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        if (mediumGen == nil) mediumGen = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
        if (heavyGen  == nil) heavyGen  = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
        if (noticeGen == nil) noticeGen = [[UINotificationFeedbackGenerator alloc] init];
        if (selectGen == nil) selectGen = [[UISelectionFeedbackGenerator alloc] init];
    }
}

// Called once at startup, while the menu is idle, so the first pulse the player feels is not
// the one UIKit drops for being unprepared.
void _SdHapticsPrepare() {
    if (@available(iOS 10.0, *)) {
        EnsureAllocated();
        [lightGen prepare]; [mediumGen prepare]; [heavyGen prepare];
        [noticeGen prepare]; [selectGen prepare];
    }
}

// `kind` is the ORDINAL of the C# Haptic enum, passed as a plain int. The mapping below is a wire
// contract with Assets/Scripts/Haptics/Haptic.cs:
//   0 Selection  1 Confirm  2 Reject  3 ImpactLight  4 ImpactHeavy
//   5 Ability    6 Reward   7 Death   8 BossRumble
// Changing the enum without changing this switch silently gives every effect the wrong feel.
void _SdHapticsPlay(int kind) {
    if (@available(iOS 10.0, *)) {
        EnsureAllocated();
        // Prepare only the generator about to fire, immediately before firing it.
        switch (kind) {
            case 0: [selectGen prepare]; [selectGen selectionChanged]; break;                                       // Selection
            case 1: [noticeGen prepare]; [noticeGen notificationOccurred:UINotificationFeedbackTypeSuccess]; break; // Confirm
            case 2: [noticeGen prepare]; [noticeGen notificationOccurred:UINotificationFeedbackTypeError];   break; // Reject
            case 3: [lightGen  prepare]; [lightGen  impactOccurred]; break;                                         // ImpactLight
            case 4: [heavyGen  prepare]; [heavyGen  impactOccurred]; break;                                         // ImpactHeavy
            case 5: [mediumGen prepare]; [mediumGen impactOccurred]; break;                                         // Ability
            case 6: [noticeGen prepare]; [noticeGen notificationOccurred:UINotificationFeedbackTypeSuccess]; break; // Reward
            case 7: [noticeGen prepare]; [noticeGen notificationOccurred:UINotificationFeedbackTypeError];   break; // Death
            case 8: [heavyGen  prepare]; [heavyGen  impactOccurred]; break;                                         // BossRumble
            default: break;
        }
    }
}

}
