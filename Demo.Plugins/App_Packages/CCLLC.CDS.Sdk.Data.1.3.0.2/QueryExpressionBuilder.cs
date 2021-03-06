using System;
using System.Linq.Expressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CCLLC.CDS.Sdk
{
    public class QueryExpressionBuilder<E> : FluentQuery<IQueryExpressionBuilder<E>,E>, IQueryExpressionBuilder<E> where E : Entity, new()
    {        
        public QueryExpressionBuilder() : base()
        {            
        }

        public QueryExpression Build()
        {
            return this.GetQueryExpression();
        }

        public IQueryExpressionBuilder<E> WithSearchValue(string searchValue)
        {
            SetSearchValue(searchValue);
            return this;
        }
    }
}
