namespace PhotoProcess
{
    public class AnalysisModeItem
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public AnalysisModeItem()
        {
            // 默认构造函数
        }

        public AnalysisModeItem(string name, string description)
        {
            Name = name;
            Description = description;
        }

        // 重写ToString方法，使Picker显示名称
        public override string ToString()
        {
            return Name;
        }
    }
}