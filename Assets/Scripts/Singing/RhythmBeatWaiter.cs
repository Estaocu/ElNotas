using System;
using System.Collections.Generic;
using UnityEngine;

public enum BeatWaitMode { Immediate, FromNextBar }

public class BeatWaitHandle
{
    internal WaitEntry Entry { get; }
    internal BeatWaitHandle(WaitEntry entry) { Entry = entry; }
    public void Cancel() => Entry.IsCancelled = true;
    public bool IsCompleted => Entry.IsCompleted;
    public bool IsCancelled => Entry.IsCancelled;
}

internal class WaitEntry
{
    public int BeatsRemaining;
    public bool WaitingForBar;
    public Action Callback;
    public bool IsCancelled;
    public bool IsCompleted;
}

public static class RhythmBeatWaiter
{
    static readonly List<WaitEntry> _active = new List<WaitEntry>();
    static readonly List<WaitEntry> _pendingAdd = new List<WaitEntry>();
    static bool _isIterating;
    static bool _subscribed;

    public static BeatWaitHandle WaitForSubBeats(int count, BeatWaitMode mode, Action callback)
    {
        if (count <= 0) throw new ArgumentException("count debe ser mayor que 0", nameof(count));
        if (callback == null) throw new ArgumentNullException(nameof(callback));

        var entry = new WaitEntry {
            BeatsRemaining = count,
            WaitingForBar  = mode == BeatWaitMode.FromNextBar,
            Callback       = callback
        };

        if (_isIterating)
            _pendingAdd.Add(entry);
        else
        {
            _active.Add(entry);
            EnsureSubscribed();
        }

        return new BeatWaitHandle(entry);
    }

    public static void CancelAll()
    {
        foreach (var e in _active) e.IsCancelled = true;
        _pendingAdd.Clear();
        _active.Clear();
        if (!_subscribed) return;
        RhythmManager.OnBeatChanged -= OnBeat;
        _subscribed = false;
    }

    static void EnsureSubscribed()
    {
        if (_subscribed) return;
        RhythmManager.OnBeatChanged += OnBeat;
        _subscribed = true;
    }

    static void OnBeat(int subBeat)
    {
        // Debug.Log($"[RhythmBeatWaiter.OnBeat] subBeat={subBeat}, active waiters={_active.Count}");
        _isIterating = true;

        for (int i = 0; i < _active.Count; i++)
        {
            var e = _active[i];
            if (e.IsCancelled || e.IsCompleted) continue;

            if (e.WaitingForBar)
            {
                if (subBeat == 1) e.WaitingForBar = false;
                continue;
            }

            e.BeatsRemaining--;
            // Debug.Log($"  [Waiter {i}] BeatsRemaining: {e.BeatsRemaining}");
            if (e.BeatsRemaining <= 0)
            {
                e.IsCompleted = true;
                // Debug.Log($"  [Waiter {i}] COMPLETED! Invoking callback");
                try { e.Callback?.Invoke(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        _isIterating = false;

        if (_pendingAdd.Count > 0)
        {
            _active.AddRange(_pendingAdd);
            _pendingAdd.Clear();
            EnsureSubscribed();
        }

        _active.RemoveAll(e => e.IsCancelled || e.IsCompleted);

        if (_active.Count == 0 && _subscribed)
        {
            RhythmManager.OnBeatChanged -= OnBeat;
            _subscribed = false;
        }
    }
}
