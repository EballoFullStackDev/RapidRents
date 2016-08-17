using RapidRents.Web.Domain;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using RapidRents.Web.Models.Requests.Comments;
using RapidRents.Data;

namespace RapidRents.Web.Services.Comments
{

    public class CommentsService : BaseService, ICommentsService
    {

        public int Insert(CommentAddRequest model, string userId)
        {
            int id = 0;

            DataProvider.ExecuteNonQuery(GetConnection, "dbo.Comment_Insert"

               , inputParamMapper: delegate (SqlParameterCollection paramCollection)
               {
                   paramCollection.AddWithValue("@TypeId", model.TypeId);
                   paramCollection.AddWithValue("@Title", model.Title);
                   paramCollection.AddWithValue("@Body", model.Body);
                   paramCollection.AddWithValue("@EntityId", model.EntityId);
                   paramCollection.AddWithValue("@userid", userId);

                   SqlParameter p = new SqlParameter("@id", System.Data.SqlDbType.Int);
                   p.Direction = System.Data.ParameterDirection.Output;

                   paramCollection.Add(p);

               }, returnParameters: delegate (SqlParameterCollection param)
               {
                   int.TryParse(param["@id"].Value.ToString(), out id);
               }
               );


            return id;
        }


        public void Update(CommentUpdateRequest model)
        {

            /*Get Connection gives connection to the database*/
            DataProvider.ExecuteNonQuery(GetConnection, "dbo.Comments_Update"

               /*Parameters from the c# to model*/
               , inputParamMapper: delegate (SqlParameterCollection paramCollection)
               {
                   paramCollection.AddWithValue("@TypeId", model.TypeId);
                   paramCollection.AddWithValue("@Title", model.Title);
                   paramCollection.AddWithValue("@Body", model.Body);
                   paramCollection.AddWithValue("@EntityId", model.EntityId);
                   paramCollection.AddWithValue("@Id", model.Id);
               });

        }


        public List<Comment> Get()
        {
            List<Comment> list = null;

            DataProvider.ExecuteCmd(GetConnection, "dbo.Comments_Select"
               , inputParamMapper: null
               , map: delegate (IDataReader reader, short set)
               {
                   Comment comment = new Comment();
                   int startingIndex = 0;

                   comment.Id = reader.GetInt32(startingIndex++);
                   comment.TypeId = reader.GetString(startingIndex++);
                   comment.Title = reader.GetString(startingIndex++);
                   comment.Body = reader.GetString(startingIndex++);
                   //comment.EntityId = reader.GetInt32(startingIndex++);
                   //comment.ParentId = reader.GetInt32(startingIndex++);
                   comment.DateAdded = reader.GetDateTime(startingIndex++);
                   comment.DateModified = reader.GetDateTime(startingIndex++);
                   comment.UserId = reader.GetString(startingIndex++);

                   if (list == null)
                   {
                       list = new List<Comment>();
                   }

                   list.Add(comment);
               }
               );
            return list;
        }


        public Comment GetById(int id)
        {
            Comment Comment = null;

            DataProvider.ExecuteCmd(GetConnection, "dbo.Comments_SelectById",
            inputParamMapper: delegate (SqlParameterCollection paramCollection)
          {
              paramCollection.AddWithValue("@Id", id);
          }
          , map: delegate (IDataReader reader, short set)
          {
              Comment = new Comment();
              int startingIndex = 0;

              Comment.Id = reader.GetInt32(startingIndex++);
              Comment.TypeId = reader.GetString(startingIndex++);
              Comment.Title = reader.GetString(startingIndex++);
              Comment.Body = reader.GetString(startingIndex++);
              Comment.EntityId = reader.GetInt32(startingIndex++);
              Comment.ParentId = reader.GetSafeInt32Nullable(startingIndex++);
              Comment.DateAdded = reader.GetDateTime(startingIndex++);
              Comment.DateModified = reader.GetDateTime(startingIndex++);
              Comment.UserId = reader.GetString(startingIndex++);

          });

            return Comment;
        }

        public List<Comment> GetById_typeId(int EntityId, string typeId)
        {
            List<Comment> list = null;

            DataProvider.ExecuteCmd(GetConnection, "dbo.Comments_SelectById_WithTypeId",
            inputParamMapper: delegate (SqlParameterCollection paramCollection)
            {
                paramCollection.AddWithValue("@EntityId", EntityId);
                paramCollection.AddWithValue("@typeId", typeId);
            }
          , map: delegate (IDataReader reader, short set)
          {
              Comment comment = new Comment();
              int startingIndex = 0;

              comment.Id = reader.GetInt32(startingIndex++);
              comment.TypeId = reader.GetString(startingIndex++);
              comment.Title = reader.GetString(startingIndex++);
              comment.Body = reader.GetString(startingIndex++);
              comment.EntityId = reader.GetInt32(startingIndex++);
              comment.ParentId = reader.GetSafeInt32Nullable(startingIndex++);
              comment.DateAdded = reader.GetDateTime(startingIndex++);
              comment.DateModified = reader.GetDateTime(startingIndex++);
              comment.UserId = reader.GetString(startingIndex++);

              if (list == null)
              {
                  list = new List<Comment>();
              }

              list.Add(comment);
          });
            return list;
        }


        public void Delete(int id)
        {
            DataProvider.ExecuteNonQuery(GetConnection, "dbo.Comments_DeleteById",
            inputParamMapper: delegate (SqlParameterCollection paramCollection)
            {
                paramCollection.AddWithValue("@Id", id);
            });
        }
    }
}