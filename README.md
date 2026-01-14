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
  <a href="https://vimeo.com/1154308580">
    <img src="https://i.vimeocdn.com/video/2106573566-c85df61ec84ebd8b798955cb5b879a4db7346386bb6ffaf11341e95124f7d0ed-d_1280.jpg" alt="UI Attachment Demo" width="100%"/>
  </a>
  <br/>
  <a href="https://vimeo.com/1154308580">
    <img src="https://img.shields.io/badge/▶_PLAY_VIDEO-00ADEF?style=for-the-badge&logo=vimeo&logoColor=white" alt="Play on Vimeo"/>
  </a>
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
