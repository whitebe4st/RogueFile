# RogueFile: Augmented Reality Investigative System

RogueFile is a Unity-based detective experience that integrates advanced hand tracking and large language model (LLM) processing to create an immersive investigative environment. This repository contains the core application logic, scripts, and documentation for the project.

## Overview

The project functions as a multi-layered detective game where players interact with the environment using physical hand gestures. It utilizes the Unity Sentis framework for high-performance, local gesture inference and the Dify platform for dynamic, context-aware NPC dialogue.

## Key Features

- Gesture-Driven Interaction: Five specialized AR minigames (Catch, Trace, Swipe, Balance, and Pour) that utilize the MediaPipe Sentis pipeline for real-time hand landmark detection.
- Advanced AI Dialogue: A centralized LLM backend (Dify) that manages NPC personalities, lore consistency, and dynamic player interactions.
- Investigative Systems: A Resident Evil-inspired grid inventory system with category-based item management (Clues vs. Items).
- High Performance: Fully GPU-accelerated hand tracking localized within Unity, eliminating the need for external Python bridges.

## Technical Stack

- Engine: Unity 2022.3+ LTS
- Inference: Unity Sentis (MediaPipe ONNX Pipeline)
- AI Backend: Dify (Advanced Chat / LLM Orchestration)
- Language: C#
- Version Control: Git LFS (for supported binary assets)

## Repository Structure

Due to the size of the original project assets (approximately 2.6 GB), this repository is configured as a "Scripts and Documentation Only" instance to maintain performance and repository health.

- /Assets/Scripts: Contains all core gameplay managers, gesture detectors, and UI controllers.
- /Assets/RogueFile.yml: The Dify application export file for importing the AI agent logic.
- /Overall_Setup_Guide.md: Detailed instructions for environment setup and project calibration.
- /minigame_flowcharts.md: Logic diagrams for all AR interaction mechanics.

## Setup Instructions

For detailed installation and configuration steps, please refer to the [Overall Setup Guide](Overall_Setup_Guide.md). This includes instructions for importing the Dify backend and configuring the Sentis GPU Compute settings.

## License

This project is developed for research and educational purposes as part of a senior project thesis.
