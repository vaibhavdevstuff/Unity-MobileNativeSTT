#import <Foundation/Foundation.h>

@interface NativeSTTPlugin : NSObject

+ (instancetype)sharedInstance;

- (void)initializeWithGameObject:(NSString *)gameObjectName language:(NSString *)language;
- (void)startListening;
- (void)stopListening;
- (void)destroy;
- (BOOL)isAvailable;

@end
