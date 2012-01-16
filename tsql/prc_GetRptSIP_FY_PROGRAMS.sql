IF EXISTS (SELECT * FROM sysobjects WHERE type = 'P' AND name = 'prc_GetRptSIP')
	DROP  Procedure prc_GetRptSIP
GO

CREATE Procedure dbo.prc_GetRptSIP
AS
BEGIN
/******************************************************************************
**		File: 
**		Name: dbo.prc_GetRptSIP
**		Desc: This is used for the "SIP ROI Database" report
**
**		This template can be customized:
**              
**		Return values:
** 
**		Called by:   
**
**		Auth: Nate Mow
**		Date: 5/11/2004
*******************************************************************************
**		Change History
*******************************************************************************
**		Date:		Author:			Description:
**		--------	--------		-------------------------------------------
**    		5/27/2004	Nate Mow		Changed @sip_investment and @actual_dollars queries WHERE clauses
*******************************************************************************/

declare @distributor_cd varchar(20)
set	@distributor_cd = 'SIP'

declare @label varchar(20)

set nocount on

create table #SIP_ROI (
	fiscal_year	int,
	rpt_section	int NOT NULL,
	label		varchar(50) NOT NULL,
	year_1		nvarchar(50), -- money
	year_2		nvarchar(50), -- money
	year_3		nvarchar(50), -- money
	year_4		nvarchar(50), -- money
	total		nvarchar(50), -- money
	SIP_investment	nvarchar(50), -- money
	ROI		nvarchar(50)  -- float
)

declare @fiscal_year int
select	@fiscal_year  = (select max(q.fiscal_year) from QUARTER q)

/*
-- Add report headers
insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total,sip_investment,roi) values((@fiscal_year+2),1,'Actual Dollars Reported','','','','','','','')
insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total,sip_investment,roi) values((@fiscal_year+2),2,'Extrapolated System-Wide Dollars','','','','','','','')
insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total,sip_investment,roi) values((@fiscal_year+2),3,'Return on Investment By Year (Actuals)','','','','','','','')
insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total,sip_investment,roi) values((@fiscal_year+2),4,'Return on Investment By Year (Extrapolated)','','','','','','','')
*/

while	@fiscal_year >= (select min(q.fiscal_year) from QUARTER q)
begin
	set @label = 'FY ' + ltrim(str(@fiscal_year)) + ' Program'
	
	declare @increment_year int
	declare @sip_investment money, @sip_investment_total money
	declare @actual_dollars money, @actual_dollars_total money, @extrap_dollars money, @extrap_dollars_total money
	declare @roi_actual float, @roi_actual_total float, @roi_extrap float, @roi_extrap_total float
	
	declare @sql_ActualDollars nvarchar(4000), @sql_ExtrapDollars nvarchar(4000), @sql_ROI_Actual nvarchar(4000), @sql_ROI_Extrap nvarchar(4000)
	
	set	@increment_year 	= @fiscal_year
	set	@sip_investment_total	= 0
	set	@actual_dollars_total	= 0
	set	@extrap_dollars_total	= 0
	set	@roi_actual_total	= 0
	set	@roi_extrap_total	= 0
	
	set	@sql_ActualDollars	= ltrim(str(@fiscal_year)) + ',1,''' + @label + ''','
	set	@sql_ExtrapDollars	= ltrim(str(@fiscal_year)) + ',2,''' + @label + ''','
	set	@sql_ROI_Actual		= ltrim(str(@fiscal_year)) + ',3,''' + @label + ''','
	set	@sql_ROI_Extrap		= ltrim(str(@fiscal_year)) + ',4,''' + @label + ''','
	
	-- "Years 2-4 should display only program_ids that are within rights and selected in Year 1"
	-- Get a static list of program_ids for Year 1
	create table #FY_PROGRAMS (program_id int NOT NULL, distributor_cd varchar(20), fiscal_year int)
	insert into #FY_PROGRAMS (program_id,distributor_cd,fiscal_year)
	(	
		/*select	pr.program_id,pr.distributor_cd,pr.fiscal_year 
		from	vw_PROGRAM_WITHIN_RIGHTS pr
		where	pr.fiscal_year = @fiscal_year 
		and	pr.distributor_cd = @distributor_cd*/
		
		select	distinct p.program_id,p.distributor_cd,@fiscal_year
		from	PROGRAM_RIGHTS	r
		join	PROGRAM		p on r.program_id = p.program_id
		join	QDR_DETAIL	d on p.program_id = d.program_id
		join	[QUARTER]	q on d.quarter_id = d.quarter_id
		--where	year(r.rights_start_dt) = @fiscal_year
		where	(	r.rights_start_dt
				between dbo.fn_FiscalYearStartDate(@fiscal_year)
				and	dbo.fn_FiscalYearEndDate(@fiscal_year)
			)
		and	p.distributor_cd = @distributor_cd
	)

	GET_DOLLARS:
	begin
		
-- Get the SIP_investments for Years 1-4 ("Only show the 1st year's SIP Investment")
		select @sip_investment =
		(
			-- Get the sum of SIP investments for @fiscal_year
			select	sum(isnull(p.sip_investment,0))
			from	PROGRAM		p
			join	#FY_PROGRAMS	fyp	on p.program_id = fyp.program_id and fyp.fiscal_year = @increment_year
		)
		--print str(@fiscal_year)+str(@increment_year)+str(@sip_investment)
		
		set @sip_investment_total = @sip_investment_total + isnull(@sip_investment,0)
		
	
-- A) Actual Dollars Reported

		-- Get the Actual Dollars for Fiscal Year @increment_year
		select @actual_dollars =
		(
			select	sum(isnull(d.on_air_dollars,0) + isnull(d.web_dollars,0))
			from	#FY_PROGRAMS	fyp
			join	QDR_DETAIL	d on d.program_id = fyp.program_id
			join	[QUARTER]	q on d.quarter_id = q.quarter_id
			where	q.fiscal_year	= @increment_year
		)
		--print	str(@fiscal_year) + ' ' + ' Year ' + ltrim(str((@increment_year-@fiscal_year)+1)) + ' ' + ltrim(str(@actual_dollars))
	
		set @actual_dollars_total = @actual_dollars_total + isnull(@actual_dollars,0)
		
		set @sql_ActualDollars = @sql_ActualDollars +	case
								when @actual_dollars is not null then ltrim(str(@actual_dollars))
								else 'null'
								end + ','
		
-- B) Extrapolated System-Wide Dollars

		-- Get the sum of per quarter Extrap Dollars for Fiscal Year @increment_year
		select @extrap_dollars =
		(
			select	sum(extrap_dollars) as [extrap_dollars]
			from	(
				select	case	when (fyp.fiscal_year <= 2002) then
						--'Actual Dollars Earned + (Actual Dollars Earned * q.extrap_rate)'
							sum(isnull(d.on_air_dollars,0) + isnull(d.web_dollars,0)) + (	
							sum(isnull(d.on_air_dollars,0) + isnull(d.web_dollars,0)) * (select q.extrap_rate from [QUARTER] q where q.quarter_id = d.quarter_id)
							)
						when (fyp.fiscal_year >= 2003 and fyp.distributor_cd = 'SIP') then
						--'Actual Dollars Earned / p.SIP_ASF'
							sum(isnull(d.on_air_dollars,0) + isnull(d.web_dollars,0)) / 
							isnull((select q.sip_asf from [QUARTER] q where q.quarter_id = d.quarter_id),1)
						when (fyp.fiscal_year >= 2003) then
						--'Actual Dollars Earned / p.NPS_ASF'
							sum(isnull(d.on_air_dollars,0) + isnull(d.web_dollars,0)) / 
							isnull((select q.nps_asf from [QUARTER] q where q.quarter_id = d.quarter_id),1)
						else 0
						end
					as
					[extrap_dollars]
				
				from	#FY_PROGRAMS	fyp
				join	QDR_DETAIL	d	on fyp.program_id = d.program_id
				join	[QUARTER]	q	on d.quarter_id = q.quarter_id and q.fiscal_year = @increment_year
				group by d.program_id,d.quarter_id,fyp.fiscal_year,fyp.distributor_cd
				)
			QDR_DETAIL
		)
		
		set @extrap_dollars_total = @extrap_dollars_total + isnull(@extrap_dollars,0)
		
		set @sql_ExtrapDollars = @sql_ExtrapDollars +	case
								when @extrap_dollars is not null then ltrim(str(@extrap_dollars))
								else 'null'
								end + ','
		
-- C) Return on Investment By Year (Actuals)
-- D) Return on Investment By Year (Extrapolated)
		if @sip_investment > 0
		begin
			select @roi_actual = (@actual_dollars / @sip_investment)
			select @roi_extrap = (@extrap_dollars / @sip_investment)
			
			set @sql_ROI_actual = @sql_ROI_actual + ltrim(cast(round(@roi_actual,2) as nvarchar(50))) + ','
			set @sql_ROI_extrap = @sql_ROI_extrap + ltrim(cast(round(@roi_extrap,2) as nvarchar(50))) + ','
		end
		else
		begin
			select @roi_actual = null
			select @roi_extrap = null
			
			set @sql_ROI_actual = @sql_ROI_actual + 'null,'
			set @sql_ROI_extrap = @sql_ROI_extrap + 'null,'
		end
		
		set @roi_actual_total = @roi_actual_total + isnull(@roi_actual,0)
		set @roi_extrap_total = @roi_extrap_total + isnull(@roi_extrap,0)
		
		
		
		-- We've hit Year 4 in the goto loop, insert @sql_[...] string
		if ((@increment_year-@fiscal_year)+1) = 4 
		begin
			-- A) Actual Dollars Reported
			set @sql_ActualDollars =
				' insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total,SIP_investment,ROI)' + char(10) + 
				' values(' + @sql_ActualDollars + ltrim(str(@actual_dollars_total)) + ',' +
					case
					when @sip_investment_total is not null then ltrim(str(@sip_investment_total))
					else 'null'
					end + 
				' ,' + ltrim(cast(round(@roi_actual_total,2) as nvarchar(50))) + ' )'
				--print @sql_ActualDollars
			
			-- B) Extrapolated System-Wide Dollars
			set @sql_ExtrapDollars =
				' insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total,SIP_investment,ROI)' + char(10) + 
				' values(' + @sql_ExtrapDollars + ltrim(str(@extrap_dollars_total)) + ',' +
					case
					when @sip_investment_total is not null then ltrim(str(@sip_investment_total))
					else 'null'
					end + 
				' ,' + ltrim(cast(round(@roi_extrap_total,2) as nvarchar(50))) + ' )'
				--print @sql_ExtrapDollars
			
			-- C) Return on Investment By Year (Actuals)
			set @sql_ROI_Actual = 
				' insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total)' + char(10) + 
				' values(' + @sql_ROI_Actual + ltrim(cast(round(@roi_actual_total,2) as nvarchar(50))) + ' )'
				--print @sql_ROI_Actual
			
			-- D) Return on Investment By Year (Extrapolated)
			set @sql_ROI_Extrap = 
				' insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total)' + char(10) + 
				' values(' + @sql_ROI_Extrap + ltrim(cast(round(@roi_extrap_total,2) as nvarchar(50))) + ' )'
				--print @sql_ROI_Extrap
			
			
			exec sp_executesql @sql_ActualDollars
			exec sp_executesql @sql_ExtrapDollars
			exec sp_executesql @sql_ROI_Actual
			exec sp_executesql @sql_ROI_Extrap
		end
		
		set @increment_year = @increment_year+1
	end
	
	
	-- Increment the Year 1-4 loop (@increment_year)
	-- [Section Title]		|	Year 1	Year 2	Year 3	Year 4	Total
	-- 'FY @fiscal_year Program'	|	------	------	------	------	-----
	if @increment_year <= (@fiscal_year+3) goto GET_DOLLARS

	drop table #FY_PROGRAMS
	
	-- Increment the @fiscal_year loop	
	select @fiscal_year = @fiscal_year-1
end


/*
-- Add the header labels
	select @fiscal_year = (select max(fiscal_year)-1 from #SIP_ROI)
	insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total,sip_investment,roi) values(@fiscal_year,1,'','First Year','Second Year','Third Year','Fourth Year','Total Revenue Pledged','SIP Investment','Total ROI')
	insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total,sip_investment,roi) values(@fiscal_year,2,'','First Year','Second Year','Third Year','Fourth Year','Total Revenue Pledged','SIP Investment','Total ROI')
	insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total,sip_investment,roi) values(@fiscal_year,3,'','First Year','Second Year','Third Year','Fourth Year','Total ROI','SIP Investment','ROI')
	insert into #SIP_ROI (fiscal_year,rpt_section,label,year_1,year_2,year_3,year_4,total,sip_investment,roi) values(@fiscal_year,4,'','First Year','Second Year','Third Year','Fourth Year','Total ROI','SIP Investment','ROI')
*/

-- The final recordset
	select	-- s.fiscal_year
		 s.rpt_section
		,s.label				as [ ]
		,case
		 when ((s.rpt_section = 1 or s.rpt_section = 2) and isnumeric(s.year_1) <> 0) then
			dbo.fn_FormatNumber(round(isnull(convert(decimal(15,6),s.year_1),0),0),0,1)
		 else	isnull(s.year_1 ,'n/a')
		 end					as [First Year]
		,case
		 when ((s.rpt_section = 1 or s.rpt_section = 2) and isnumeric(s.year_2) <> 0) then
			dbo.fn_FormatNumber(round(isnull(convert(decimal(15,6),s.year_2),0),0),0,1)
		 else	isnull(s.year_2 ,'n/a')
		 end					as [Second Year]
		,case
		 when ((s.rpt_section = 1 or s.rpt_section = 2) and isnumeric(s.year_3) <> 0) then
			dbo.fn_FormatNumber(round(isnull(convert(decimal(15,6),s.year_3),0),0),0,1)
		 else	isnull(s.year_3 ,'n/a')
		 end					as [Third Year]
		,case
		 when ((s.rpt_section = 1 or s.rpt_section = 2) and isnumeric(s.year_4) <> 0) then
			dbo.fn_FormatNumber(round(isnull(convert(decimal(15,6),s.year_4),0),0),0,1)
		 else	isnull(s.year_4 ,'n/a')
		 end					as [Fourth Year]
		,case
		 when ((s.rpt_section = 1 or s.rpt_section = 2) and isnumeric(s.total) <> 0) then
			dbo.fn_FormatNumber(round(isnull(convert(decimal(15,6),s.total),0),0),0,1)
		 else	isnull(s.total ,'n/a')
		 end					as [Total]
		,case
		 when ((s.rpt_section = 1 or s.rpt_section = 2) and isnumeric(s.SIP_investment) <> 0) then
			dbo.fn_FormatNumber(round(isnull(convert(decimal(15,6),s.SIP_investment),0),0),0,1)
		 else	isnull(s.SIP_investment ,'n/a')
		 end					as [SIP Investment]
		,isnull(cast(s.ROI as nvarchar(50))			,'n/a')	as [ROI]
	from	#SIP_ROI s
	order by	 s.rpt_section
			,s.fiscal_year desc
	
	drop table #SIP_ROI

set nocount off
END
GO

GRANT EXEC ON dbo.prc_GetRptSIP TO PUBLIC
GO
