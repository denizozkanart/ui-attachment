 ## UI Attachment System (Unity 2022.3.37f1 LTS)

This project demonstrates a functional weapon attachment UI built in Unity, featuring category switching, dynamic card strips, stat updates, and a real-time preview system.

---

## 🔧 What Works

- **Attachment Categories:** Sight / Mag / Barrel / Stock / Tactical  
- **Card Strip:** Selecting a card instantly updates the weapon model  
- **Equip State:** Selection persists when switching categories  
- **Stats Panel:** Row values and colors update based on stat deltas  
- **Preview Camera:** Predefined poses + smooth fit-to-node transitions    

---

## 📁 Code Location
Assets/Task1_UI/Scripts/

Main controller: **AttachmentManager.cs**  
(Handles UI flow, card population, stat syncing, and preview transitions.)

---

## ▶️ How to Test

1. Open the scene:
Assets/Task1_UI/Scenes/UI_Attachment.unity
2. Press **Play**  
3. Click different categories → card strip updates  
4. Select a card → weapon model + stats update accordingly  

---

## 🖼 UI Attachment Preview

Below are three preview screenshots showcasing the UI Attachment system in action:

<p align="center">
  <img src="Screenshots/ui_attachment_preview(0).png" width="32%" />
  <img src="Screenshots/ui_attachment_preview(1).png" width="32%" />
  <img src="Screenshots/ui_attachment_preview(2).png" width="32%" />
</p>

---

## 🎬 Demo Video

<div align="center">
  <iframe title="vimeo-player" src="https://player.vimeo.com/video/1154308580?h=1b813a6281" width="640" height="360" frameborder="0" allow="autoplay; fullscreen; picture-in-picture" allowfullscreen></iframe>
</div>

I built this attachment UI system from scratch in Unity. It handles category switching, card-based selection, stat updates, and camera transitions. Everything is modular so it's easy to expand later.

---

## ✔ Notes

- Designed for clarity and modularity — all logic is separated by responsibility.  
- Variables kept inspector-friendly for quick iteration during production.  
- Runs on **Unity 2022.3.37f1 LTS** (as used in the case study).

---

## 👤 Author  
**Deniz Özkan**  
Technical Artist / 3D Artist  
GitHub: https://github.com/denizozkanart

---
