<div align="center">

# 🌑 FocusMask | 影·幕

**Project "Ying": A Native Focus Spotlight for Windows**
<br>
*聚光于此 · 隐入暗影 · 沉浸专注*

<!-- 统一使用 Miku绿 (#39C5BB) 风格徽章 -->
[![Language](https://img.shields.io/badge/Language-C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://dotnet.microsoft.com/)
[![Framework](https://img.shields.io/badge/Framework-WPF%20%2F%20.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-39C5BB?style=for-the-badge&logo=windows&logoColor=white)](https://www.microsoft.com)

</div>

---

## 📖 Introduction (简介)

**FocusMask** (Code Name: *Project Ying*) is a lightweight productivity tool designed to reduce visual distractions on Windows.
It automatically dims everything on your screen except the **active window**, creating a "Spotlight" effect that follows your focus.

**影·幕** 是一款 Windows 全局专注工具。它会自动将非活动窗口压暗，只高亮当前正在操作的窗口。配合丝滑的动画过渡，助你屏蔽纷扰，迅速进入心流状态。

---

## 💡 Inspiration (致敬)

> *"Bringing the HazeOver experience to Windows."*

**FocusMask** is a tribute to the legendary macOS utility, **[HazeOver](https://hazeover.com/)**.
We love the "dimming" concept on Mac, so we built a native, open-source alternative for Windows users.

**致敬 macOS 平台的神级工具 HazeOver。**
我们希望能将这种“聚光灯”般的沉浸式专注体验带给 Windows 用户，让多窗口工作不再杂乱。

---

## ✨ Features (功能亮点)

### 🔦 Auto Spotlight (自动聚光)
- **Smart Detection:** Automatically detects the foreground window and cuts a "hole" in the dark mask.
- **DWM Precision:** Uses `DwmGetWindowAttribute` to accurately calculate window bounds (ignoring invisible drop shadows).

### 🌊 Fluid Experience (丝滑体验)
- **Smooth Animation:** Window transitions are interpolated (Lerp), making the spotlight "slide" rather than jump.
- **Mouse Passthrough:** The dark layer uses `WS_EX_TRANSPARENT`, meaning it is purely visual. You can still click through the darkness to select other windows.

### ⌨️ Geek Controls (极客交互)
- **Double Tap `Alt`:** Quickly toggle the effect On/Off. (双击 Alt 开关)
- **`Alt` + Mouse Wheel:** Adjust the dimming opacity on the fly. (Alt + 滚轮调节亮度)

---

## 🛠️ Tech Stack (技术栈)

Built with **C# & WPF**, utilizing low-level Windows Hooks for maximum performance.

| Component | Technology | Description |
| :--- | :--- | :--- |
| **UI Framework** | ![WPF](https://img.shields.io/badge/-WPF-512BD4?style=flat-square&logo=dotnet&logoColor=white) | For vector-based masking and rendering. |
| **Window Hooks** | ![Win32](https://img.shields.io/badge/-Win32_API-0078D6?style=flat-square&logo=windows&logoColor=white) | `SetWinEventHook` to track window focus changes. |
| **Input Hooks** | ![Hooks](https://img.shields.io/badge/-LowLevel_Hooks-39C5BB?style=flat-square) | `WH_KEYBOARD_LL` & `WH_MOUSE_LL` for global hotkeys. |

---

## 🚀 How to Run (使用方法)

### Option 1: Download
Download the latest portable `.exe` from [**Releases**](https://github.com/Linkium-suki/FocusMask/releases).

### Option 2: Build from Source
1.  Open `FocusMask.sln` in **Visual Studio**.
2.  Select `Release` configuration.
3.  Build solution (`Ctrl+Shift+B`).
4.  Run the generated executable. No installation required.

---

## 📄 License

This project is licensed under the **Apache License 2.0**.

<br>

<div align="center">

*Made with ❤️ by [Linkium](https://github.com/Linkium-suki)*

</div>
