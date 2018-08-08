
using Urho;

namespace UrhoCustomObj.Components
{
    public class Box : Urho.Shapes.Box
    {
        public Box()
        {
            
        }

        public override void OnAttachedToNode(Node node)
        {
            base.OnAttachedToNode(node);

            this.Color = Color.Blue;
            Node.SetScale(.5f);


        }

        
    }
}
