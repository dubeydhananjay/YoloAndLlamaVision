# 🦙🔍 Yolo + LLaMA Vision Mixed Reality System

This project is an end-to-end **AI-powered Mixed Reality experience** combining:

- **YOLOv9** for real-time object detection.
- **Meta LLaMA Vision-Instruct** for language descriptions.
- **Unity** for visualization and speech interaction.
- **Flask API Server** to orchestrate processing.

The system enables users to:
- Detect objects through camera input in a Mixed Reality environment.
- Generate natural language descriptions of detected objects.
- Interact via voice commands.

---

## ✨ Features

✅ **Real-Time Object Detection**
- Multi-scale YOLOv9 detection.
- Cropped bounding box previews.
- Display detection results in Unity.

✅ **Natural Language Descriptions**
- Generate detailed captions with LLaMA Vision-Instruct.
- Answer follow-up questions via speech.

✅ **Unity Integration**
- Holographic bounding boxes.
- Detection grids.
- Speech recognition (DictationRecognizer).
- Text-to-speech playback.

✅ **REST API Server**
- /process endpoint for detection.
- /generate_description for LLaMA responses.

✅ **Mixed Reality Ready**
- Works with HoloLens and other devices using MRTK.

---

## 🧩 System Architecture

```
[Unity C# App]
   |
   |-- Captures camera image
   |-- Sends image to Flask API via HTTP
   |-- Receives detection results
   |-- Visualizes bounding boxes + grids
   |-- Accepts voice input ("start", "next")
   |-- Sends prompt + image to /generate_description
   |-- Speaks generated response
   |
[Flask API Server]
   |
   |-- YOLOv9 detection (PyTorch)
   |-- LLaMA text generation
```

---

## 🛠️ Server Setup

> 🖥️ **Python 3.8+ and CUDA-compatible GPU recommended**

1️⃣ **Clone the repository**
```bash
git clone git@github.com:dubeydhananjay/YoloAndLlamaVisionServer.git
cd YoloAndLlamaVision
```

2️⃣ **Create virtual environment**
```bash
python -m venv venv
source venv/bin/activate
```

3️⃣ **Install dependencies**
```bash
pip install -r requirements.txt
```

4️⃣ **Download YOLOv9 weights**
Download `yolov9t.pt` from [YOLOv9 repo](https://github.com/WongKinYiu/yolov9)  
Put it in:
```
yolov9/yolov9t.pt
```

5️⃣ **Configure LLaMA access**
Ensure you have access to:
```
meta-llama/Llama-3.2-11B-Vision-Instruct
```
Login to Hugging Face if needed:
```bash
huggingface-cli login
```

6️⃣ **Start the server**
```bash
python main_server.py
```

✅ Runs on:
```
http://localhost:5001
```

---

## 🕶️ Unity Project Overview

The Unity side is responsible for:

- Capturing camera frames (`CameraDataCollector2`)
- Sending images to `/process`
- Displaying bounding boxes
- Rendering detection grids (`DetectionGridPanel`)
- Handling speech commands (`LlamaVisionSpeechRecogniserNew`)
- Requesting LLaMA descriptions
- Reading responses with Text-to-Speech

**Key C# scripts:**

| Script                           | Purpose                                      |
|----------------------------------|----------------------------------------------|
| `YoloDetection.cs`               | Upload image to server, get detections      |
| `CameraDataCollector2.cs`        | Capture images from webcam                  |
| `LlamaVisionSpeechRecogniserNew` | Dictation and prompt sending                |
| `DetectionGridPanel.cs`          | Create grid of detected objects             |

---

## 🎯 API Reference

### 🟢 POST `/process`
Detect objects in an image.

**Query:**
```
type=image
```
**Form Data:**
- `file`: image/jpeg

**Response:**
- `objects`: List of detections (class, confidence, box coords, cropped_image)
- `image`: Base64-encoded annotated image

---

### 🟢 POST `/generate_description`
Generate text description.

**Form Data:**
- `prompt`: Your query text
- `image`: image/jpeg

**Response:**
- `description`: Text response from LLaMA

---

## 🕹️ Unity Usage

1️⃣ **Configure `serverUrl`**
Edit:
- `YoloDetection.cs`
- `LlamaVisionSpeechRecogniserNew.cs`

Set:
```
https://6f5a-128-235-248-168.ngrok-free.app/process?type=image for yolo detection
https://6f5a-128-235-248-168.ngrok-free.app/generate_description for llama vision
```

2️⃣ **Run Scene**
- Press Play
- Start detection via script or button
- Use voice commands:
  - "start": begin dictation
  - "next": stop speech and continue

3️⃣ **Results**
- Detected objects appear in `DetectionGridPanel`
- LLaMA descriptions shown and read aloud

---

## ⚙️ Requirements

- Unity 2020.3+ or newer
- MRTK (Mixed Reality Toolkit)
- Python 3.8+
- GPU for YOLOv9 and LLaMA acceleration
- Hugging Face LLaMA access

---

## 🧠 Acknowledgements

- [YOLOv9](https://github.com/WongKinYiu/yolov9)
- Meta AI LLaMA
- Hugging Face Transformers
- Unity + MRTK

---

## 📄 License

MIT License

---

## 🙌 Contributing

PRs and issues are welcome!

---

## ✉️ Contact

Questions? [Open an Issue](https://github.com/dubeydhananjay/YoloAndLlamaVision/issues).

---

