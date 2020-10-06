using System;
using System.Collections.Generic;
using System.Text;
using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.Contracts.Entity
{
    public class MxHostDeleted : Message
    {
        public MxHostDeleted(string id) : base(id)
        {
        }
    }
}
