Camp Hero

Project Overview
This project is a voxel-based survival game built in Unity where players gather resources, build structures, and develop a growing camp over time. The game features procedurally generated terrain using a custom voxel system, along with mechanics for exploration, crafting, and survival.

Purpose
The goal of this project is to explore efficient voxel rendering and world generation techniques while building a scalable survival game system. It also serves as a way to practice system design in game development, including chunk-based terrain, procedural generation, and gameplay systems like building and NPC management.

Technologies Used
Unity (C#)
Custom procedural mesh generation
Chunk-based voxel terrain system
Noise functions for terrain generation
Object pooling for performance optimization
Setup & Installation

Clone the repository:
git clone https://github.com/TegaU16/Camp-Hero.git
Open the project in Unity Hub.
Use a compatible Unity version (recommended: Unity 2022+).
Open the main scene and press Play.

Features
Procedural voxel terrain with chunk-based generation
Optimized mesh generation (only visible faces rendered)
Grid-based building system with placement preview (“ghost” objects)
Camp progression system with upgradable structures
Inventory and storage system (e.g., chests)
Dynamic environment elements (e.g., day/night cycle)
Multi-chunk structure support (e.g., villages, ruins)
Performance optimizations using object pooling

My Contribution
I designed and implemented the core systems of the game, including:
Procedural voxel terrain generation using chunked meshes
Face culling system to render only visible voxel faces
Building placement system with snapping and validation
Storage and interaction systems (e.g., chests, furnaces)
Structure spawning system that supports multi-chunk placement
Performance improvements through pooling and efficient updates

Screenshots / Demo
(Add screenshots, GIFs, or gameplay clips here)
