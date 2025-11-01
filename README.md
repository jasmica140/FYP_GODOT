# Developing an Engaging Metroidvania Game Through Procedural Content Generation

## 🎮 Overview
This project explores how **procedural content generation (PCG)** can be used to design **engaging and handcrafted-feeling level layouts** for games in the **Metroidvania genre**.  
The goal was to develop a system capable of generating **interesting, varied, and structurally coherent rooms** while maintaining the sense of deliberate design typical of hand-crafted Metroidvania games.

The project was implemented in **Godot (C#)** and focuses on:
- Structuring procedural level generation using **zones, primitives, and anchors**.
- Building **paths between doors** using **anchor-based pathfinding**.
- Introducing **environmental logic** like keys, locks, and hazards.
- Evaluating the **interestingness** of generated levels using statistical techniques.

---

## 🧩 Features
- **Zone-Based Room Generation:**  
  Splits each room into rectangular subareas, each containing floors, hazards, and objects.
- **Anchor System:**  
  Defines spatial relationships between primitives for valid placement and path connections.
- **Primitive and Atom Hierarchy:**  
  Each primitive (e.g., pit, ladder, slope) is composed of one or more atoms (e.g., tiles).
- **Path Building:**  
  Connects start and end doors through reachable primitives while avoiding obstructions.
- **Lock and Key System:**  
  Generates colour-coded key-lock pairs for puzzle-style progression.
- **Interestingness Evaluation:**  
  Quantifies level quality using metrics derived from a **Monte Carlo simulation**.

---

## 🧠 Technical Architecture
- **Engine:** Godot 4 (C#)
- **Language:** C#
- **Evaluation:** Python (for data analysis and Monte Carlo simulation)
- **Version Control:** GitHub
- **Tools:**  
  - Visual Studio Code / Rider  
  - Godot Engine  
  - LaTeX for dissertation formatting

---

## Ilustrations of Some Player Abilities

- **Jumping**
<img width="1390" height="635" alt="Jumping" src="https://github.com/user-attachments/assets/576c25c5-e725-42de-bd7c-8cf5ccc52dbf" />

- **Climbing**
<img width="927" height="452" alt="Climbing" src="https://github.com/user-attachments/assets/9e88efe6-f199-4597-a39a-2e39c8ccfb53" />

- **Rolling**
<img width="524" height="310" alt="Screenshot 2025-11-01 at 18 35 27" src="https://github.com/user-attachments/assets/9b24b662-653f-48fc-9737-29a732a15305" />

- **Hurt Recoil**
<img width="696" height="371" alt="Screenshot 2025-11-01 at 18 31 39" src="https://github.com/user-attachments/assets/220dfd9d-c1d2-4032-ae7d-aa3288fda827" />

- **Swimming**
<img width="739" height="590" alt="Screenshot 2025-11-01 at 18 31 25" src="https://github.com/user-attachments/assets/66045270-83f6-465d-8498-f2ccf9543f64" />

---

## 🏗️ System Structure
/project_root
│
├── /Scenes
│   ├── Player/
│   ├── Primitives/
│   ├── Room/
│   └── ZoneHandler/
│
├── /Scripts
│   ├── PlayerController.cs
│   ├── Primitive.cs
│   ├── Atom.cs
│   ├── ZoneHandler.cs
│   ├── PathBuilder.cs
│   └── InterestingnessEvaluator.py
│
├── /Assets
│   ├── Sprites/
│   ├── Tilesets/
│   └── Fonts/
│
└── README.md

---

## ⚙️ How It Works

1. **Zone Generation:**  
   Rooms are first divided into rectangular *zones* using a BSP algorithm.

2. **Primitive Placement:**  
   Each zone is filled with primitives (e.g., floors, slopes, ladders) that meet structural rules.

3. **Anchor Connectivity:**  
   Anchors define points where primitives can connect. Overlapping orbits form valid links.

4. **Path Building:**  
   Anchor connections form a graph used to create continuous paths between door primitives.

5. **Environmental Features:**  
   After paths are formed, hazards, collectibles, and lock-key pairs are added.

6. **Evaluation:**  
   Levels are assessed for variety, coherence, and interest through Monte Carlo analysis.

---

## 📊 Evaluation and Results
A **Monte Carlo simulation** was conducted to analyse how different parameters (e.g., room size, number of doors, difficulty) affect interestingness scores.  
The system demonstrated consistent generation of structurally sound and varied maps.

<img width="1439" height="840" alt="finalroomexample4" src="https://github.com/user-attachments/assets/33bc6d68-7c01-4fb7-8f91-61133a807c85" />

<img width="1438" height="840" alt="finalroomexample3" src="https://github.com/user-attachments/assets/a485bb8a-c891-4e60-a1e1-65ca342cec7e" />

<img width="1594" height="700" alt="boxplot" src="https://github.com/user-attachments/assets/0bfd0592-376d-4d32-b62b-4e124c850c17" />

---

## 📈 Future Improvements
- Add enemy and item placement logic.  
- Expand the interestingness evaluation to include **player engagement metrics**.  
- Introduce **thematic consistency layers** for visuals (e.g., biome-based tilesets).  
- Extend pathfinding to **multi-room map generation**.

---

## 📚 References
- Reddit Thread: ["Procedural Generation in Metroidvanias"](https://www.reddit.com/r/metroidvania/comments/kqpxn6/procedural_generation_in_metroidvanias/)  
- Togelius, J., et al. (2011). *Procedural Content Generation: Goals, Challenges, and Actionable Steps.*  
- Smith, G. et al. (2011). *An Analysis of Dungeon Crawl Stone Soup’s Procedural Level Generation.*  
- Shaker, N., Togelius, J., & Nelson, M. (2016). *Procedural Content Generation in Games.*

---

## 👩‍💻 Author
**Jasmine Micallef**  
B.Sc. (Hons.) in Computer Science — University of Malta  
Year: 2025

---

## 🏁 Acknowledgements
Special thanks to my tutors for their feedback and support throughout the project.

---
