using Playnite.SDK;

namespace Bangumi.Models
{
    public class SearchOption : GenericItemOption
    {
        public uint Id { get; set; }
        

        public SearchOption(uint id, string name, string description) : base(name, description)
        {
            this.Id = id;
        }

        public override string ToString()
        {
            return $"PlayniteSearchOption{{{Id}, {Name}, {Description}}}";
        }
    }
}