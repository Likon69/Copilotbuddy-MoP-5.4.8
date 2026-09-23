namespace Bots.Professionbuddy
{
	public interface IDeepCopy<out T> 
	{
		T DeepCopy();
	}
}
