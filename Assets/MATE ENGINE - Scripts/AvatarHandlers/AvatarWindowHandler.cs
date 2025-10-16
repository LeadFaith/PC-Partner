﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class AvatarWindowHandler : MonoBehaviour
{
    public int snapThreshold = 30, verticalOffset = 0;
    public float desktopScale = 1f;
    [Header("Pink Snap Zone (Unity-side)")]
    public Vector2 snapZoneOffset = new(0, -5);
    public Vector2 snapZoneSize = new(100, 10);

    [Header("Window Sit BlendTree")]
    public int totalWindowSitAnimations = 4;
    private static readonly int windowSitIndexParam = Animator.StringToHash("WindowSitIndex");
    private bool wasSitting = false;

    [Header("User Y-Offset Slider")]
    [Range(-0.015f, 0.015f)]
    public float windowSitYOffset = 0f;

    [Header("Fine-Tune")]
    float snapFraction;
    public float baseOffset = 40f;
    public float baseScale = 1f;
    int _snapCursorY;
    bool wasDragging;
    bool dragStartedInsideBand;


    IntPtr snappedHWND = IntPtr.Zero, unityHWND = IntPtr.Zero;
    Vector2 snapOffset;
    Vector2 lastDesktopPosition;
    readonly List<WindowEntry> cachedWindows = new();
    Rect pinkZoneDesktopRect;
    // float snapFraction, baseScale = 1f, baseOffset = 40f;
    Animator animator;
    AvatarAnimatorController controller;
    readonly System.Text.StringBuilder classNameBuffer = new(256);

    void Start()
    {
        unityHWND = Process.GetCurrentProcess().MainWindowHandle;
        animator = GetComponent<Animator>();
        controller = GetComponent<AvatarAnimatorController>();
        SetTopMost(true);
    }
    void Update()
    {
        if (unityHWND == IntPtr.Zero || animator == null || controller == null) return;
        if (!SaveLoadHandler.Instance.data.enableWindowSitting) return;

        bool isSittingNow = animator != null && animator.GetBool("isWindowSit");
        if (isSittingNow && !wasSitting)
        {
            int sitIdx = UnityEngine.Random.Range(0, totalWindowSitAnimations);
            animator.SetFloat(windowSitIndexParam, sitIdx);
        }
        wasSitting = isSittingNow;

        var unityPos = GetUnityWindowPosition();
        UpdateCachedWindows();
        UpdatePinkZone(unityPos);

        if (controller.isDragging && !wasDragging && snappedHWND != IntPtr.Zero && animator.GetBool("isWindowSit"))
        {
            Kirurobo.WinApi.POINT cp;
            if (Kirurobo.WinApi.GetCursorPos(out cp)) _snapCursorY = cp.y;
        }




        if (controller.isDragging && !controller.animator.GetBool("isSitting"))
        {
            if (snappedHWND == IntPtr.Zero)
                TrySnap(unityPos);
            else if (!IsStillNearSnappedWindow())
            {
                snappedHWND = IntPtr.Zero;
                animator.SetBool("isWindowSit", false);
                SetTopMost(true);
            }
            else
                FollowSnappedWindowWhileDragging();
        }
        else if (!controller.isDragging && snappedHWND != IntPtr.Zero)
            FollowSnappedWindow();

        if (snappedHWND != IntPtr.Zero)
        {
            foreach (var win in cachedWindows)
            {
                if (win.hwnd == snappedHWND && (IsWindowMaximized(win.hwnd) || IsWindowFullscreen(win)))
                {
                    MoveMateToDesktopPosition();

                    snappedHWND = IntPtr.Zero;
                    if (animator != null)
                    {
                        animator.SetBool("isWindowSit", false);
                        animator.SetBool("isSitting", false);
                    }
                    SetTopMost(true);
                    break;
                }
            }
        }

        if (animator != null && animator.GetBool("isBigScreenAlarm"))
        {
            if (animator.GetBool("isWindowSit"))
            {
                animator.SetBool("isWindowSit", false);
            }
            snappedHWND = IntPtr.Zero;
            SetTopMost(true);
            return;
        }
        wasDragging = controller.isDragging;
    }
    void UpdateCachedWindows()
    {
        cachedWindows.Clear();
        EnumWindows((hWnd, lParam) =>
        {
            if (!IsWindowVisible(hWnd) || !GetWindowRect(hWnd, out RECT r)) return true;
            classNameBuffer.Clear();
            GetClassName(hWnd, classNameBuffer, classNameBuffer.Capacity);
            string cls = classNameBuffer.ToString();
            bool isTaskbar = cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd";
            if (!isTaskbar)
            {
                if ((r.Right - r.Left) < 100 || (r.Bottom - r.Top) < 100) return true;
                if (GetParent(hWnd) != IntPtr.Zero || GetWindowTextLength(hWnd) == 0) return true;
                if (cls == "Progman" || cls == "WorkerW" || cls == "DV2ControlHost" || cls == "MsgrIMEWindowClass" ||
                    cls.StartsWith("#") || cls.Contains("Desktop")) return true;
            }
            cachedWindows.Add(new WindowEntry { hwnd = hWnd, rect = r });
            return true;
        }, IntPtr.Zero);
    }

    void UpdatePinkZone(Vector2 unityPos)
    {
        float cx = unityPos.x + GetUnityWindowWidth() * 0.5f + snapZoneOffset.x;
        float by = unityPos.y + GetUnityWindowHeight() + snapZoneOffset.y;
        pinkZoneDesktopRect = new Rect(cx - snapZoneSize.x * 0.5f, by, snapZoneSize.x, snapZoneSize.y);
    }

    void TrySnap(Vector2 unityWindowPosition)
    {
        foreach (var win in cachedWindows)
        {
            if (win.hwnd == unityHWND) continue;
            int barMidX = win.rect.Left + (win.rect.Right - win.rect.Left) / 2, barY = win.rect.Top + 2;
            var pt = new POINT { X = barMidX, Y = barY };
            if (GetAncestor(WindowFromPoint(pt), GA_ROOT) != win.hwnd) continue;
            var topBar = new Rect(win.rect.Left, win.rect.Top, win.rect.Right - win.rect.Left, 5);
            if (!pinkZoneDesktopRect.Overlaps(topBar)) continue;
            lastDesktopPosition = GetUnityWindowPosition();
            snappedHWND = win.hwnd;
            float winWidth = win.rect.Right - win.rect.Left, unityWidth = GetUnityWindowWidth();
            float petCenterX = unityWindowPosition.x + unityWidth * 0.5f;
            snapFraction = (petCenterX - win.rect.Left) / winWidth;
            snapOffset.y = GetUnityWindowHeight() + snapZoneOffset.y + snapZoneSize.y * 0.5f;
            animator.SetBool("isWindowSit", true);
            SetTopMost(false);
            Kirurobo.WinApi.POINT cp;
            if (Kirurobo.WinApi.GetCursorPos(out cp)) _snapCursorY = cp.y;
            return;
        }
    }
    void FollowSnappedWindowWhileDragging()
    {
        foreach (var win in cachedWindows)
        {
            if (win.hwnd != snappedHWND) continue;

            var unityPos = GetUnityWindowPosition();
            float unityWidth = GetUnityWindowWidth();
            float winWidth = win.rect.Right - win.rect.Left;
            float petCenterX = unityPos.x + unityWidth * 0.5f;
            snapFraction = Mathf.Clamp01((petCenterX - win.rect.Left) / winWidth);

            float yOffset = GetUnityWindowHeight() + snapZoneOffset.y + snapZoneSize.y * 0.5f;
            float scale = transform.localScale.y, scaleOffset = (baseScale - scale) * baseOffset;
            float windowSitOffset = windowSitYOffset * GetUnityWindowHeight();
            int targetY = win.rect.Top - (int)(yOffset + scaleOffset) + verticalOffset + Mathf.RoundToInt(windowSitOffset);

            SetUnityWindowPosition(Mathf.RoundToInt(unityPos.x), targetY);
            return;
        }
    }




    void FollowSnappedWindow()
    {
        foreach (var win in cachedWindows)
        {
            if (win.hwnd != snappedHWND) continue;
            float winWidth = win.rect.Right - win.rect.Left, unityWidth = GetUnityWindowWidth();
            float newCenterX = win.rect.Left + snapFraction * winWidth;
            int targetX = Mathf.RoundToInt(newCenterX - unityWidth * 0.5f);
            float yOffset = GetUnityWindowHeight() + snapZoneOffset.y + snapZoneSize.y * 0.5f;
            float scale = transform.localScale.y, scaleOffset = (baseScale - scale) * baseOffset;
            float windowSitOffset = windowSitYOffset * GetUnityWindowHeight();
            int targetY = win.rect.Top - (int)(yOffset + scaleOffset) + verticalOffset + Mathf.RoundToInt(windowSitOffset);
            SetUnityWindowPosition(targetX, targetY);
            SetWindowPos(unityHWND, win.hwnd, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            return;
        }
        snappedHWND = IntPtr.Zero;
        animator.SetBool("isWindowSit", false);
        SetTopMost(true);
    }

    bool IsStillNearSnappedWindow()
    {
        foreach (var win in cachedWindows)
        {
            if (win.hwnd != snappedHWND) continue;

            if (controller.isDragging && animator.GetBool("isWindowSit"))
            {
                Kirurobo.WinApi.POINT cp;
                if (!Kirurobo.WinApi.GetCursorPos(out cp)) return true;
                int dy = Mathf.Abs(cp.y - _snapCursorY);
                return dy <= Mathf.RoundToInt(snapZoneSize.y);
            }

            return pinkZoneDesktopRect.Overlaps(new Rect(win.rect.Left, win.rect.Top, win.rect.Right - win.rect.Left, 5));
        }
        return false;
    }




    [DllImport("user32.dll")]
    static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }
    const int SW_MAXIMIZE = 3;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)] static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT pt);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", SetLastError = true)] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true)] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr hWnd);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)] static extern int GetWindowTextLength(IntPtr hWnd);
    public struct RECT { public int Left, Top, Right, Bottom; }
    public struct POINT { public int X, Y; }
    struct WindowEntry { public IntPtr hwnd; public RECT rect; }
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    static readonly IntPtr HWND_TOPMOST = new(-1), HWND_NOTOPMOST = new(-2);
    const uint GA_ROOT = 2, SWP_NOMOVE = 0x0002, SWP_NOSIZE = 0x0001, SWP_NOACTIVATE = 0x0010;
    void SetTopMost(bool en) => SetWindowPos(unityHWND, en ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    Vector2 GetUnityWindowPosition() { GetWindowRect(unityHWND, out RECT r); return new(r.Left, r.Top); }
    int GetUnityWindowWidth() { GetWindowRect(unityHWND, out RECT r); return r.Right - r.Left; }
    int GetUnityWindowHeight() { GetWindowRect(unityHWND, out RECT r); return r.Bottom - r.Top; }
    void SetUnityWindowPosition(int x, int y) => MoveWindow(unityHWND, x, y, GetUnityWindowWidth(), GetUnityWindowHeight(), true);

    bool IsWindowMaximized(IntPtr hwnd)
    {
        WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
        placement.length = Marshal.SizeOf(placement);
        if (GetWindowPlacement(hwnd, ref placement))
            return placement.showCmd == SW_MAXIMIZE;
        return false;
    }

    bool IsWindowFullscreen(WindowEntry win)
    {
        int width = win.rect.Right - win.rect.Left;
        int height = win.rect.Bottom - win.rect.Top;
        int screenWidth = Display.main.systemWidth;
        int screenHeight = Display.main.systemHeight;
        int tolerance = 2;
        return Mathf.Abs(width - screenWidth) <= tolerance && Mathf.Abs(height - screenHeight) <= tolerance;
    }
    void MoveMateToDesktopPosition()
    {
        int x = Mathf.RoundToInt(lastDesktopPosition.x);
        int y = Mathf.RoundToInt(lastDesktopPosition.y);
        SetUnityWindowPosition(x, y);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        float basePixel = 1000f / desktopScale;
        Gizmos.color = Color.magenta; DrawDesktopRect(pinkZoneDesktopRect, basePixel);
        GetWindowRect(unityHWND, out RECT uRect);
        Gizmos.color = Color.green; DrawDesktopRect(new Rect(uRect.Left, uRect.Bottom - 5, uRect.Right - uRect.Left, 5), basePixel);
        foreach (var win in cachedWindows)
        {
            if (win.hwnd == unityHWND) continue;
            int w = win.rect.Right - win.rect.Left, h = win.rect.Bottom - win.rect.Top;
            Gizmos.color = Color.red; DrawDesktopRect(new Rect(win.rect.Left, win.rect.Top, w, 5), basePixel);
            Gizmos.color = Color.yellow; DrawDesktopRect(new Rect(win.rect.Left, win.rect.Top, w, h), basePixel);
        }
    }

    void DrawDesktopRect(Rect r, float basePixel)
    {
        float cx = r.x + r.width * 0.5f, cy = r.y + r.height * 0.5f;
        int screenWidth = Display.main.systemWidth, screenHeight = Display.main.systemHeight;
        float unityX = (cx - screenWidth * 0.5f) / basePixel, unityY = -(cy - screenHeight * 0.5f) / basePixel;
        Vector3 worldPos = new(unityX, unityY, 0), worldSize = new(r.width / basePixel, r.height / basePixel, 0);
        Gizmos.DrawWireCube(worldPos, worldSize);
    }

    public void ForceExitWindowSitting()
    {
        snappedHWND = IntPtr.Zero;
        if (animator != null)
        {
            animator.SetBool("isWindowSit", false);
            animator.SetBool("isSitting", false);
        }
        SetTopMost(true);
    }
    bool CursorWithinSnapBandOfSnappedWindow()
    {
        foreach (var win in cachedWindows)
        {
            if (win.hwnd != snappedHWND) continue;
            Kirurobo.WinApi.POINT cp;
            if (!Kirurobo.WinApi.GetCursorPos(out cp)) return true;
            int band = Mathf.RoundToInt(snapZoneSize.y);
            int top = win.rect.Top;
            if (cp.y < top - band || cp.y > top + 5 + band) return false;
            return true;
        }
        return true;
    }

}