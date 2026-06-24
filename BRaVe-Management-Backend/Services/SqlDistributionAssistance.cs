using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BRaVe_Management_Backend.Services
{
    public class SqlDistributionAssistanceService : IDistributionAssistanceService
    {
        private string connectionString { get; set; }
        private readonly ILogger<SqlDistributionAssistanceService> logger;
        public SqlDistributionAssistanceService(ISecretProvider secretProvider) {

            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
                
                //config.GetConnectionString("DefaultConnection");
        }
 
        public async Task CreateDistributionAssistance(DistributionAssistanceDto data, string UserId)
        {

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
              
                var cmd = new SqlCommand(@"sp_CreateDistributionAssistance @KitId,@Quantity,@Notes,@DistributionID,@ItemTag,@TenantId,@CreatedByUserId", conn);
     

                cmd.Parameters.AddWithValue("@KitId", data.KitId);
                cmd.Parameters.AddWithValue("@Quantity", data.Quantity);
                cmd.Parameters.AddWithValue("@Notes", data.Notes);
                cmd.Parameters.AddWithValue("@DistributionID", data.DistributionID);
                //cmd.Parameters.AddWithValue("@ItemTagID", data.ItemTagID);
                cmd.Parameters.AddWithValue("@ItemTag", data.ItemTag);

                cmd.Parameters.AddWithValue("@TenantId", data.TenantId);
 

                cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while creating Kit Type for '{DistributionID}'", data.DistributionID);
                throw;
            }
           
        }

        public async Task UpdateDistributionAssistance(DistributionAssistance data, string UserId)
        {

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var cmd = new SqlCommand(@"sp_UpdateDistributionAssistance @Id,@KitId,@Quantity,@ItemTag,@Notes,@UpdatedByUserId", conn);

                cmd.Parameters.AddWithValue("@Id", data.Id);
                cmd.Parameters.AddWithValue("@KitId", data.KitId);
                cmd.Parameters.AddWithValue("@Quantity", data.Quantity);
                //cmd.Parameters.AddWithValue("@ItemTagID", data.ItemTagID);
                cmd.Parameters.AddWithValue("@ItemTag", data.ItemTag);

                cmd.Parameters.AddWithValue("@Notes", data.Notes);
 
 

                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while Updating  Distribution Type kit'{KitId}'", data.KitId);
                throw;
            }

        }

      
        public async Task<IEnumerable<DistributionAssistance>> GetAllDistributionAssistance(int TenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT d.Id,d.TenantId,d.KitId,d.Notes, d.Quantity,d.CreatedOn,d.CreatedByUserId, d.UpdatedOn,k.Name KitName,d.DistributionID,ItemTag " +
                    "from tbl_DistributionAssistances d inner join tbl_DistributionKits k on k.Id=d.KitId where d.TenantId=@TenantId";


                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", TenantId);

                List<DistributionAssistance> DistributionAssistances = new List<DistributionAssistance>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var distributionAssistance = new DistributionAssistance()
                        {

                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            KitId = reader.GetInt32(reader.GetOrdinal("KitId")),
                             Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        
                            Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                            

                            TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                            KitName = reader.GetString(reader.GetOrdinal("KitName")),
                            DistributionID = reader.GetInt32(reader.GetOrdinal("DistributionID")),
                            //ItemTagID = reader.GetInt32(reader.GetOrdinal("ItemTagID")),
                            ItemTag = reader.GetString(reader.GetOrdinal("ItemTag")),

                            UpdatedOn = reader.IsDBNull(reader.GetOrdinal("CreatedOn")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedOn")),

                           };
                        DistributionAssistances.Add(distributionAssistance);

                    }
                }

                return DistributionAssistances;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving Distribution Kit type with tenantid={TenantId}", TenantId);
            }

            return null;
        }
         

         







   
    
 

        public Task<DistributionAssistance?> GetDistributionAssistanceById(int id)
        {
            throw new NotImplementedException();
        }

       
        public Task GetDistributionKitByDistributionAssistanceId(int DistributionId)
        {
            throw new NotImplementedException();
        }

     
    }
}
