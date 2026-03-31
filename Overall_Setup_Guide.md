# 📁 Project RogueFile — Overall Setup Guide

This guide covers the end-to-end setup for the AR Detective project, including the AI backend and hand-tracking systems.

---

## 🏗️ 1. Project Prerequisites

*   **Unity Version:** Recommended 2022.3 LTS or higher.
*   **MediaPipe Sentis:** The project uses Unity Sentis for GPU-accelerated hand tracking.
*   **Dify (Self-hosted or Cloud):** Used for the LLM Agent backend.
*   **Git LFS:** Must be installed before cloning/pushing (Assets folder is **~2.6 GB**).

---

## 🤖 2. Dify Backend Setup (LLM)

The conversational intelligence of NPCs (Grimm, Suspects) is managed via **Dify**.

### Importing the Application:
1.  Navigate to your Dify workspace.
2.  Go to **Explore** or **App List** and click **Import DSL**.
3.  Upload the file: `Assets/RogueFile.yml`.
4.  This will create an "Advanced Chat" application with the following predefined logic:
    *   **Grimm Personality:** Hard-boiled noir detective ("The Reaper").
    *   **Suspect Personalities:** Pre-configured responses for Act 1 lore.
    *   **Knowledge Base:** Connected to the project's lore documents for consistent fact-checking.

---

## 🕶️ 3. Unity AR & Sentis Setup

The project uses MediaPipe Sentis for high-performance gesture detection without a Python bridge.

### Hand Tracking Configuration:
1.  Locate the `HandDetection` script in the scene.
2.  **Backend Mode:** Ensure it is set to `GPUCompute` for best performance (prevents stuttering during minigames).
3.  **Dify Integration:** In the `DifyManager` component, update your **API Key** and **URL** to match your imported Dify application.

---

## 🕵️ 4. Clue Minigames (Overview)

Your progress is gated by 5 interactive minigames:
1.  **Clue 1 (Fist Catch):** Reaction test to catch a falling page.
2.  **Clue 2 (Trace):** Memory test tracing a scratch pattern with the index finger.
3.  **Clue 3 (Swipe):** Categorization test sorting fan mail via wrist velocity.
4.  **Clue 4 (Balance):** Precision test balancing a folder using palm tilt (Pitch/Roll).
5.  **Clue 5 (Pour):** Nerve test pouring liquid using wrist rotation (Roll).

---

## 📦 5. Git & Storage Management

The project is configured with a strict `.gitignore` and `.gitattributes` for efficient version control.

*   **Library Folder (~6.6 GB):** Ignored automatically. Do not commit.
*   **Assets (~2.6 GB):** Tracked using **Git LFS** for ONNX models, 3D models, and textures.
*   **Documentation:** Most generic `.md` and `.pdf` files are ignored to keep the repository lean. Only this `Overall_Setup_Guide.md` is un-ignored by default.

---

> [!IMPORTANT]
> **Always ensure Git LFS is active** when committing new `.onnx` models or high-resolution textures, or you will hit repository size limits.
