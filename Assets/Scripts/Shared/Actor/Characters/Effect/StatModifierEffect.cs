
using MessagePack;

[MessagePackObject]
public class StatModifierEffect
{
    [Key(0)]
    public bool Enable { get; set; } = true;

    [Key(1)]
    public float MaxDuration { get; set; }

    [Key(2)]
    public float ElapsedTime { get; set; }
    public void Update()
    {
        ElapsedTime += WorldTime.DeltaTime;
        if (ElapsedTime >= MaxDuration)
        {
            Enable = false;
        }
    }
}