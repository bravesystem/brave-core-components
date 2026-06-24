namespace BRaVe_Management_Backend.Models
{
    public class CriteriaFieldExtractor
    {
        public static HashSet<string> GetReferencedFields(RuleDefinition criteria)
        {
            var fields = new HashSet<string>();
            if (criteria?.Rules == null) return fields;

            foreach (var rule in criteria.Rules)
            {
                if (rule?.Criteria != null)
                    fields.UnionWith(GetReferencedFieldsFromNode(rule.Criteria));
            }
            return fields;
        }

        public static HashSet<string> GetReferencedFields(CriteriaNode criteria)
        {
            var fields = new HashSet<string>();

            if (criteria?.Rules == null) {

                if(!string.IsNullOrEmpty(criteria.FieldName))
                    fields.Add(criteria.FieldName);

                return fields;
            }
                

            fields.UnionWith(GetReferencedFieldsFromNode(criteria));

            return fields;
        }

        private static HashSet<string> GetReferencedFieldsFromNode(CriteriaNode node)
        {
            var fields = new HashSet<string>();
            if (node == null) return fields;

            if (!string.IsNullOrEmpty(node.Operator) && node.Rules != null)
            {
                foreach (var r in node.Rules)
                    fields.UnionWith(GetReferencedFieldsFromNode(r));
            }
            else if (!string.IsNullOrEmpty(node.FieldName))
            {
                fields.Add(node.FieldName);
            }
            return fields;
        }
    }
}
