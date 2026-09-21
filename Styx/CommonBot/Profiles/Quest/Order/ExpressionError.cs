namespace Styx.CommonBot.Profiles.Quest.Order
{
    public class ExpressionError
    {
        internal ExpressionError(string expression, string error)
        {
            Error = error;
            Expression = expression;
        }

        public string Expression { get; private set; }

        public string Error { get; private set; }

        public override string ToString()
        {
            return string.Format("Error in expression '{0}': {1}", Expression, Error);
        }
    }
}
