using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using RapidRents.Web.Domain.State;

namespace RapidRents.Web.Services.States
{
    public class StatesService : BaseService, IStatesService
    {
        public List<Territory> GetByCountryCode(string CountryRegionCode)
        {
            List<Territory> list = null; 

            DataProvider.ExecuteCmd(GetConnection, "dbo.StateProvince_GetByCountryRegionCode",  
           inputParamMapper: delegate (SqlParameterCollection paramCollection)
           {
               paramCollection.AddWithValue("@CountryRegionCode", CountryRegionCode);
           }
              , map: delegate (IDataReader reader, short set)
               {
                   Territory state = new Territory();
                   int startingIndex = 0;

                   state.StateProvinceId = reader.GetInt32(startingIndex++);
                   state.StateProvinceCode = reader.GetString(startingIndex++);
                   state.CountryRegionCode = reader.GetString(startingIndex++);
                   state.IsOnlyStateProvinceFlag = reader.GetBoolean(startingIndex++);
                   state.Name = reader.GetString(startingIndex++);
                   state.TerritoryId = reader.GetInt32(startingIndex++);   
                   state.rowGuid = reader.GetGuid(startingIndex++);
                   state.ModifiedDate = reader.GetDateTime(startingIndex++);


                   if (list == null)
                   {
                       list = new List<Territory>(); 
                   }

                   list.Add(state);
               }
               );

            return list;
        }
    }
}