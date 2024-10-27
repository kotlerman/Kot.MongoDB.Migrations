﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApiApp.Net6.Documents;

internal class User
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }
    public string Login { get; set; }
}
