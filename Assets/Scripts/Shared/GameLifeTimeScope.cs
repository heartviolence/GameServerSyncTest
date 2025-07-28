
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    public WorldReference b;
    protected override void Configure(IContainerBuilder builder)
    {
        builder.UseEntryPoints(Lifetime.Singleton, entryPoints =>
        {
            entryPoints.Add<EntryPoint>();
        });

        builder.RegisterComponent(b);
        builder.Register<ServerWorldStates>(Lifetime.Singleton);
        builder.Register<ActorPreloader>(Lifetime.Singleton); 
    }
}