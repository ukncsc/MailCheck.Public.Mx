﻿using System;
using System.Collections.Generic;

namespace MailCheck.Mx.SimplifiedTlsTester.Smtp
{
    public class SmtpResponse
    {
        public SmtpResponse(List<Response> responses)
        {
            Responses = responses;
        }

        public List<Response> Responses { get; }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, Responses);
        }
    }
}