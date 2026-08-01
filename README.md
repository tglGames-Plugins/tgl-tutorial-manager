# Tutorial Handler
A simple Tutorial Handler. we can use it to define the type of tutorial we want to play on a canvas.

## Limitations
- If we have dynamic content, we may need to wait a few frames to use the service as dynamic data needs time to settle down
- in case of dynamic content, deletion of items needs to be managed by the user.
- If we have a scrollable layout where we are trying to show data, the scroll capablity needs to be stopped by the user
- If we have animations or items that need user to wait, we have to call block instructions


### How to Use
To use this plugin, follow these steps:
- Users will communicate to the system through TutorialHandler.cs file, you can change code to use MessageBus if that helps.
- call events in TutorialHandler class to use the tutorial system.
- if you do not want to highlight a rect transform, call BlockScreenEvent event.
- if you want to highlight a gameobject, 
	- get rect transforms of the gameobjects that you want to emphasis during the tutorial
	- Pass the rect transforms to ShowTutorialEvent event
- Once done, call HideTutorialEvent event
- In case of canvas not updating smoothly, call RefreshTutorialCanvasEvent event to force refresh the tutorial canvas
- In case you need to stop the tutorial temporarily, for popup or other reasons, you can use InturreptTutorialEvent event

## Samples
Samples can be found in "Samples" folder. There is a scene with the test setup.

## How to add this package?
See [How to add this package](HowToAddPackage.md) for detailed instructions.
