import { usePageCommand } from "@kentico/xperience-admin-base";
import {
  Button,
  ButtonColor,
  Icon,
  Spacing,
} from "@kentico/xperience-admin-components";
import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";

const { Portal, TemplateSideMenu } =
  require("@kentico/xperience-admin-base") as {
    Portal: React.ComponentType<{
      container: HTMLElement | null;
      children: React.ReactNode;
    }>;
    TemplateSideMenu: React.ComponentType<Record<string, never>>;
  };
import {
  ReactFlow,
  Background,
  useNodesState,
  useEdgesState,
  Handle,
  Position,
  MarkerType,
  BaseEdge,
  type Node,
  type Edge,
  type NodeTypes,
  type NodeProps,
  type EdgeTypes,
  type EdgeProps,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import styles from "./AutomationBuilderTemplate.module.css";

/* ------------------------------------------------------------------ */
/*  Types                                                              */
/* ------------------------------------------------------------------ */

interface AutomationProcessNodeStatisticDto {
  iconName: string;
  value: number;
  statisticTooltip?: string | null;
}

interface AutomationProcessNodeDto {
  id: string;
  name: string;
  stepType: string;
  iconName: string;
  isSaved: boolean;
  statistics: AutomationProcessNodeStatisticDto[];
  configuration?: Record<string, unknown> | null;
}

interface AutomationProcessConnectionDto {
  id: string;
  sourceNodeGuid: string;
  targetNodeGuid: string;
  sourceHandle?: string | null;
}

interface LoadAutomationProcessResult {
  nodes: AutomationProcessNodeDto[];
  connections: AutomationProcessConnectionDto[];
}

interface AutomationStatisticsProps {
  lastStatisticsRecalculationDateTime: string;
  lastStatisticsRecalculationTooltipTemplate: string;
  lastStatisticsRecalculationLabelTemplate: string;
  recalculateButtonLabel: string;
  recalculatingButtonLabel: string;
}

/* ------------------------------------------------------------------ */
/*  Constants                                                          */
/* ------------------------------------------------------------------ */

const NODE_WIDTH = 260;
const NODE_HEIGHT = 62;
const NODE_SPACING_Y = 60;
const BADGE_HEIGHT = 28;
const BRANCH_OFFSET = 160;

/* ------------------------------------------------------------------ */
/*  Statistics node — read-only with badge                             */
/* ------------------------------------------------------------------ */

interface StatNodeData {
  label: string;
  iconName: string;
  stepType: string;
  isTrigger: boolean;
  statistics: AutomationProcessNodeStatisticDto[];
  [key: string]: unknown;
}

function StatNodeComponent({ data }: NodeProps<Node<StatNodeData>>) {
  const handleStyle: React.CSSProperties = {
    background: "#8c8c8c",
    borderWidth: 0,
    pointerEvents: "none" as const,
  };

  return (
    <div className={styles.nodeWrapper}>
      {!data.isTrigger && (
        <Handle type="target" position={Position.Top} style={handleStyle} />
      )}
      {data.isTrigger && (
        <div className={styles.startingLineLabel}>Starting line</div>
      )}
      <div className={`${styles.stepNode} ${styles.stepNodeDisabled}`}>
        <span className={styles.iconBorder}>
          <Icon name={data.iconName as any} />
        </span>
        <div style={{ display: "flex", flexDirection: "column" }}>
          <span>{data.label}</span>
          {/* Statistics badges — hidden for Condition nodes (matches native) */}
          {data.stepType !== "Condition" &&
            data.statistics &&
            data.statistics.length > 0 && (
              <div style={badgeRowStyle}>
                {data.statistics.map((stat, i) => (
                  <div
                    key={i}
                    style={badgeStyle}
                    title={stat.statisticTooltip ?? ""}
                  >
                    <Icon name={stat.iconName as any} />
                    <span>{stat.value}</span>
                  </div>
                ))}
              </div>
            )}
        </div>
        {/* Condition handles: ✅/❌ icons rendered INSIDE the Handle (matches native) */}
        {data.stepType === "Condition" && (
          <>
            <Handle
              type="source"
              position={Position.Bottom}
              id="true"
              className={styles.conditionSourceTrue}
              style={{
                left: "40%",
                bottom: "-8px",
              }}
            >
              <svg
                width="8"
                height="8"
                viewBox="0 0 16 16"
                fill="none"
                style={{ display: "block" }}
              >
                <path
                  fillRule="evenodd"
                  clipRule="evenodd"
                  d="M14.833 2.139a.58.58 0 0 1 .04.77L5.935 13.818a.484.484 0 0 1-.362.182.48.48 0 0 1-.369-.165L1.143 9.29a.581.581 0 0 1 .009-.771.471.471 0 0 1 .707.01l3.689 4.126 8.58-10.473a.471.471 0 0 1 .706-.043Z"
                  fill="currentColor"
                />
              </svg>
            </Handle>
            <Handle
              type="source"
              position={Position.Bottom}
              id="false"
              className={styles.conditionSourceFalse}
              style={{
                left: "60%",
                bottom: "-8px",
              }}
            >
              <svg
                width="8"
                height="8"
                viewBox="0 0 16 16"
                fill="none"
                style={{ display: "block" }}
              >
                <path
                  fillRule="evenodd"
                  clipRule="evenodd"
                  d="M1.854 1.146a.5.5 0 0 0-.708.708l6.146 6.145-6.146 6.145a.5.5 0 0 0 .708.707l6.145-6.145 6.145 6.145a.5.5 0 0 0 .707-.707L8.706 7.999l6.145-6.145a.5.5 0 1 0-.707-.707L8 7.292 1.854 1.146Z"
                  fill="currentColor"
                />
              </svg>
            </Handle>
          </>
        )}
      </div>
      {data.stepType !== "Finish" &&
        data.stepType !== "End" &&
        data.stepType !== "Condition" && (
          <Handle
            type="source"
            position={Position.Bottom}
            style={handleStyle}
          />
        )}
    </div>
  );
}

/* Edge that handles both straight vertical and L-shaped (branch) paths */
function StatEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  markerEnd,
  style,
}: EdgeProps) {
  const dx = targetX - sourceX;
  const dy = targetY - sourceY;

  let edgePath: string;

  if (Math.abs(dx) < 1) {
    // Straight vertical edge
    edgePath = `M${sourceX} ${sourceY}L${sourceX} ${targetY}`;
  } else {
    // L-shaped edge: DOWN → HORIZONTAL → DOWN
    const dir = Math.sign(dx);
    const turnY = sourceY + Math.min(15, dy * 0.35);
    const r = Math.min(
      10,
      Math.abs(dx) / 2,
      Math.max(1, turnY - sourceY),
      Math.max(1, targetY - turnY),
    );
    edgePath = [
      `M${sourceX} ${sourceY}`,
      `L ${sourceX},${turnY - r}`,
      `Q ${sourceX},${turnY} ${sourceX + dir * r},${turnY}`,
      `L ${targetX - dir * r},${turnY}`,
      `Q ${targetX},${turnY} ${targetX},${turnY + r}`,
      `L${targetX} ${targetY}`,
    ].join("");
  }

  return (
    <BaseEdge id={id} path={edgePath} markerEnd={markerEnd} style={style} />
  );
}

const nodeTypes: NodeTypes = { statNode: StatNodeComponent as any };
const edgeTypes: EdgeTypes = { statEdge: StatEdge as any };

/* ------------------------------------------------------------------ */
/*  Badge styles                                                       */
/* ------------------------------------------------------------------ */

const badgeRowStyle: React.CSSProperties = {
  display: "flex",
  gap: "6px",
  justifyContent: "center",
  marginTop: "4px",
};

const badgeStyle: React.CSSProperties = {
  display: "inline-flex",
  alignItems: "center",
  gap: "4px",
  padding: "2px 8px",
  borderRadius: "100px",
  background: "#f0f0f0",
  color: "#495057",
  fontSize: "12px",
  fontWeight: 600,
  height: `${BADGE_HEIGHT}px`,
  lineHeight: `${BADGE_HEIGHT}px`,
};

/* ------------------------------------------------------------------ */
/*  Header label style                                                 */
/* ------------------------------------------------------------------ */

const headerLabelStyle: React.CSSProperties = {
  fontSize: "13px",
  color: "var(--color-text-low-emphasis, #8b8b8b)",
};

/* ------------------------------------------------------------------ */
/*  Component                                                          */
/* ------------------------------------------------------------------ */

function formatDateTime(iso: string): string {
  if (!iso) return "";
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return iso;
  }
}

export const AutomationStatisticsTemplate = (
  props: AutomationStatisticsProps,
) => {
  const [processNodes, setProcessNodes] = useState<AutomationProcessNodeDto[]>(
    [],
  );
  const [processConnections, setProcessConnections] = useState<
    AutomationProcessConnectionDto[]
  >([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRecalculating, setIsRecalculating] = useState(false);
  const [lastRecalc, setLastRecalc] = useState(
    props.lastStatisticsRecalculationDateTime,
  );

  const [rfNodes, setRfNodes, onRfNodesChange] = useNodesState([]);
  const [rfEdges, setRfEdges, onRfEdgesChange] = useEdgesState([]);

  const headerRef = useRef(document.getElementById("applicationHeader"));

  const { execute: loadProcess } = usePageCommand<LoadAutomationProcessResult>(
    "LoadAutomationProcess",
    {
      after: (result) => {
        if (result) {
          setProcessNodes(result.nodes);
          setProcessConnections(result.connections);
        }
        setIsLoading(false);
      },
    },
  );

  const { execute: recalculate } = usePageCommand("RecalculateStatistics", {
    after: () => {
      setIsRecalculating(false);
      setLastRecalc(new Date().toISOString());
      // Reload graph with fresh stats
      loadProcess();
    },
  });

  // Load on mount
  useEffect(() => {
    loadProcess();
  }, []);

  // Build React Flow nodes/edges from process data
  useEffect(() => {
    if (processNodes.length === 0) {
      setRfNodes([]);
      setRfEdges([]);
      return;
    }

    const nodeMap = new Map(processNodes.map((n) => [n.id, n]));
    const forwardMap = new Map<
      string,
      Array<{ target: string; sourceHandle?: string | null }>
    >();
    const incomingSet = new Set<string>();
    for (const conn of processConnections) {
      const arr = forwardMap.get(conn.sourceNodeGuid) || [];
      arr.push({
        target: conn.targetNodeGuid,
        sourceHandle: conn.sourceHandle,
      });
      forwardMap.set(conn.sourceNodeGuid, arr);
      incomingSet.add(conn.targetNodeGuid);
    }

    const flowNodes: Node[] = [];
    const flowEdges: Edge[] = [];
    const visitedNodes = new Set<string>();

    const layoutSubtree = (
      nodeId: string,
      x: number,
      y: number,
      parentId: string | null,
      parentSourceHandle?: string | null,
    ): number => {
      if (visitedNodes.has(nodeId)) return y;
      visitedNodes.add(nodeId);

      const node = nodeMap.get(nodeId);
      if (!node) return y;

      flowNodes.push({
        id: nodeId,
        type: "statNode",
        position: { x: x - NODE_WIDTH / 2, y },
        data: {
          label: node.name,
          iconName: node.iconName,
          stepType: node.stepType,
          isTrigger: node.stepType === "Trigger" || node.stepType === "Start",
          statistics: node.statistics,
        } satisfies StatNodeData,
        draggable: false,
        selectable: false,
      });

      // Edge from parent
      if (parentId) {
        const edgeId = parentSourceHandle
          ? `${parentId}-[${parentSourceHandle}]->${nodeId}`
          : `${parentId}->${nodeId}`;
        flowEdges.push({
          id: edgeId,
          source: parentId,
          sourceHandle: parentSourceHandle || undefined,
          target: nodeId,
          type: "statEdge",
          markerEnd: {
            type: MarkerType.Arrow,
            width: 20,
            height: 20,
            color: "var(--color-border-default)",
          },
          style: {
            stroke: "var(--color-border-default, #8c8c8c)",
            strokeWidth: 1,
          },
        } as Edge);
      }

      let nextY = y + NODE_HEIGHT + BADGE_HEIGHT + NODE_SPACING_Y;
      const children = forwardMap.get(nodeId) || [];

      if (node.stepType === "Condition") {
        const trueChild = children.find((c) => c.sourceHandle === "true");
        const falseChild = children.find((c) => c.sourceHandle === "false");

        if (trueChild && nodeMap.has(trueChild.target)) {
          nextY = layoutSubtree(
            trueChild.target,
            x - BRANCH_OFFSET,
            nextY,
            nodeId,
            "true",
          );
        }

        if (falseChild && nodeMap.has(falseChild.target)) {
          const falseY = layoutSubtree(
            falseChild.target,
            x + BRANCH_OFFSET,
            y + NODE_HEIGHT + BADGE_HEIGHT + NODE_SPACING_Y,
            nodeId,
            "false",
          );
          nextY = Math.max(nextY, falseY);
        }
      } else if (children.length > 0) {
        nextY = layoutSubtree(children[0].target, x, nextY, nodeId);
      }

      return nextY;
    };

    const centerX = 0;
    let y = 0;

    // Find root (no incoming) and walk graph
    const rootIds = processNodes
      .filter((n) => !incomingSet.has(n.id))
      .map((n) => n.id);
    if (rootIds.length > 0) {
      layoutSubtree(rootIds[0], centerX, y, null);
    }

    setRfNodes(flowNodes);
    setRfEdges(flowEdges);
  }, [processNodes, processConnections]);

  const handleRecalculate = useCallback(async () => {
    setIsRecalculating(true);
    await recalculate();
  }, [recalculate]);

  const statsLabel = useMemo(() => {
    if (!lastRecalc) return "";
    const formatted = formatDateTime(lastRecalc);
    if (props.lastStatisticsRecalculationLabelTemplate) {
      return props.lastStatisticsRecalculationLabelTemplate.replace(
        "{0}",
        formatted,
      );
    }
    return `Statistics from: ${formatted}`;
  }, [lastRecalc, props.lastStatisticsRecalculationLabelTemplate]);

  const statsTooltip = useMemo(() => {
    if (!lastRecalc) return "";
    const formatted = formatDateTime(lastRecalc);
    if (props.lastStatisticsRecalculationTooltipTemplate) {
      return props.lastStatisticsRecalculationTooltipTemplate.replace(
        "{0}",
        formatted,
      );
    }
    return `Last recalculated: ${formatted}`;
  }, [lastRecalc, props.lastStatisticsRecalculationTooltipTemplate]);

  if (isLoading) {
    return (
      <div style={{ padding: "2rem", textAlign: "center", color: "#8b8b8b" }}>
        Loading statistics…
      </div>
    );
  }

  return (
    <>
      {/* Header portal — Stats date + Refresh button */}
      <Portal container={headerRef.current}>
        <div style={{ display: "flex", alignItems: "center" }}>
          <div style={{ padding: "0 0 0 16px" }}>
            <span style={headerLabelStyle} title={statsTooltip}>
              {statsLabel}
            </span>
          </div>
          <div style={{ padding: "0 0 0 16px" }}>
            <Button
              label={
                isRecalculating
                  ? props.recalculatingButtonLabel || "Recalculating…"
                  : props.recalculateButtonLabel || "Recalculate"
              }
              onClick={handleRecalculate}
              color={ButtonColor.Secondary}
              icon="xp-rotate-right"
              disabled={isRecalculating}
            />
          </div>
        </div>
      </Portal>

      {/* Canvas */}
      <div className={styles.canvasFlexWrapper}>
        <div className={styles.canvasWrapper}>
          <ReactFlow
            nodes={rfNodes}
            edges={rfEdges}
            onNodesChange={onRfNodesChange}
            onEdgesChange={onRfEdgesChange}
            nodeTypes={nodeTypes}
            edgeTypes={edgeTypes}
            fitView
            fitViewOptions={{ padding: 0.3 }}
            nodesDraggable={false}
            nodesConnectable={false}
            elementsSelectable={false}
            panOnDrag
            zoomOnScroll
            minZoom={0.3}
            maxZoom={1.5}
            proOptions={{ hideAttribution: true }}
          >
            <Background color="#f3f3f3" gap={16} />
          </ReactFlow>
        </div>
      </div>
      <TemplateSideMenu />
    </>
  );
};

export const AutomationStatistics = AutomationStatisticsTemplate;
