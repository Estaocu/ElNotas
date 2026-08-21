using System;
using System.Collections.Generic;
using UnityEngine;

public class RhythmScheduler : MonoBehaviour
{
    private class ScheduledEvent
    {
        public double dspTime;
        public Action callback;
        public long order;
    }

    private readonly List<ScheduledEvent> scheduledEvents = new List<ScheduledEvent>();

    private long nextOrder;

    private void Update()
    {
        ProcessScheduledEvents();
    }

    public void ScheduleAt(double dspTime, Action callback)
    {
        if (callback == null)
            return;

        ScheduledEvent scheduledEvent = new ScheduledEvent
        {
            dspTime = dspTime,
            callback = callback,
            order = nextOrder++
        };

        InsertEvent(scheduledEvent);
    }

    public void CancelAll()
    {
        scheduledEvents.Clear();
    }

    private void ProcessScheduledEvents()
    {
        if (scheduledEvents.Count == 0)
            return;

        double currentDspTime = AudioSettings.dspTime;

        while (scheduledEvents.Count > 0)
        {
            ScheduledEvent scheduledEvent = scheduledEvents[0];

            if (scheduledEvent.dspTime > currentDspTime)
                break;

            scheduledEvents.RemoveAt(0);

            scheduledEvent.callback?.Invoke();
        }
    }

    private void InsertEvent(ScheduledEvent scheduledEvent)
    {
        int low = 0;
        int high = scheduledEvents.Count;

        while (low < high)
        {
            int middle = (low + high) / 2;

            ScheduledEvent currentEvent = scheduledEvents[middle];

            if (currentEvent.dspTime < scheduledEvent.dspTime)
            {
                low = middle + 1;
            }
            else if (currentEvent.dspTime > scheduledEvent.dspTime)
            {
                high = middle;
            }
            else if (currentEvent.order < scheduledEvent.order)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        scheduledEvents.Insert(low, scheduledEvent);
    }
}