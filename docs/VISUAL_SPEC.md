# Fruit Tree visual acceptance specification

## Art direction
Relaxing, carefully composed, semi-realistic 3D orchard tree; not photorealistic and not toy-like plastic. The player should feel they are standing in front of a naturally shaped apple tree, not browsing a store UI. A single tree stays central.

## Composition
- Portrait 9:16 through 9:20. Tree occupies ~70-85% of playable frame height while zoomed out, with trunk and ground visible.
- Background must be low contrast: light sky gradient, simple grassy clearing; no pathways, fences, houses, extra trees or props.
- The HUD is just apple progress plus small Restart and Sound buttons; it cannot obscure the tree crown.
- Sunlight warm but not overexposed. Ground shadow soft. No bloom or heavy blur.

## The tree
- Irregular tapered bark/branches. Avoid obvious repeated straight cylinders, stretched spheres and disconnected limb joints in the final visual.
- Leaves have varied angles and shades; canopy retains gaps through which to inspect apples.
- A few leaves sway in the wind (very small displacement). Apples stay visually attached to twigs.
- Most fruit reachable from an outside orbit, some partially hidden among leaves.
- Apples should have recognizable lobed red form, restrained glossy highlights and stems that actually connect each apple to its twig from all camera angles.

## Game feel
- Swipe horizontally: continuous 360-degree camera orbit, no snapping or screen jumps.
- Vertical swipe: adjust camera elevation within comfortable limits.
- Pinch: inspect branches; two-finger slide raises / lowers the focus. Verify clipping behavior on device.
- Tap a ripe apple: counted exactly once, immediate visual and sound response.
- No timer, energy, lives, ads, in-app purchases or distracting prompts.

## Mobile testing targets (not verified)
- 30 FPS with downgraded visuals on constrained Android devices; 60 FPS target on mid-range devices.
- Check 20:9 tall display as well as 16:9.
- No unreadable text, lost touches, excessive overheating, or abnormal battery drain.
- Camera and tap tests on real devices required before declaring release-ready.

**Art status:** the committed generator is a functional baseline. Runtime screenshot review and visual refinement in Unity are outstanding. Do not confuse the previously approved image mockup with a verified real-time Unity screenshot.
