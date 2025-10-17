# FactoryGameV2
This is a fixed and updated version of a private project I made years ago, using a simple template for a factory game. It uses Unity v2021.1.26f1. The original template was an FPS that I've converted into an isometric game with the same mechanics.
My eventual goal with this game is to completely get rid of the factory models and make something closer to a city-builder that's like Simcity with agents moving between buildings/junctures on conveyer belts. I liked the idea of having 
a city-builder that was represented through a factory floor, where systemic flow and issues are represented through physics and floor planning. Blocks will fall off the belt if they have nowhere to go. I have a lot of ideas with how you can
mix these concepts (on messily scrawled A3 sketch pads) that I hope to someday implement.

## Controls/How to Play ##

C Button - Toggle Between Belt and Structure Building Modes
TAB - Toggle structures 

All structures have an output (red node) and an input (green node) that can be connected via conveyer belts. Conveyer belts can be placed by clicking on an output and then clicking on an input. The input system for belts is inconsistent and I find that sometiems clicking-and-dragging works, 
other times clicking on two points works. This is likely a glitch due to the camera change perspective making the raycast detection less reliable.

To start, build a Mining/Spawning structure, a connector and a Container. The moment you link a Spawner and a Connector, blocks will spawn on the conveyer belt and move towards their destination. If it's a connector, and the output isn't linked 
to anything else, the blocks will simply fall off the belt.

