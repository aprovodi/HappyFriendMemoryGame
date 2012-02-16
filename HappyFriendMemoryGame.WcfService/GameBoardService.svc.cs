using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace HappyFriendMemoryGame.WcfService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "GameBoardService" in code, svc and config file together.
    public class GameBoardService : IGameBoardService
    {
        private Board _board;

        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public Board getNewBoard(int value)
        {
            this._board = new Board(value);
            return this._board;
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }
    }
}
