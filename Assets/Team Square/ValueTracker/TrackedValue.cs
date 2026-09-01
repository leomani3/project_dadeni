using System;

[Serializable]
public class TrackedValue
{
    public double value;
    
    public TrackedValue(TrackedValueType trackedValueType, double initialValue = 0f)
    {
        value = initialValue;
    }
    
    public void SetValue(double newValue)
    {
        value = newValue;
    }
    
    public void Increment(double amount)
    {
        value += amount;
    }
    
    public void Reset()
    {
        value = 0f;
    }
}
