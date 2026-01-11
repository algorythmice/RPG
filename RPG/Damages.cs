namespace RPG;

public class Damages
{
    public int CalculateDamage(int damage, int health)
    {
        return health - damage;
    }
    
}