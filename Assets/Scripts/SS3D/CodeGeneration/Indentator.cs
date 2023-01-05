using System.Text;

namespace SS3D.CodeGeneration
{
    public class Indentator
    {
        private StringBuilder _builder;

        private int _lastAmount = 0;
        private int _amount = 0;

        public Indentator()
        {
            _builder = new StringBuilder();
        }

        public void Add()
        {
            _amount++;
        }

        public void Sub()
        {
            _amount--;
        }

        public override string ToString()
        {
            if (_lastAmount == _amount)
            {
                return _builder.ToString();
            }

            _builder.Clear();
            for (int i = 0; i < _amount; i++)
            {
                _builder.Append("    ");
            }

            _lastAmount = _amount;
            return _builder.ToString();
        }
    }
}